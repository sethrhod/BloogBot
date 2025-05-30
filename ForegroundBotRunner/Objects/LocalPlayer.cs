﻿using ForegroundBotRunner.Mem;
using GameData.Core.Enums;
using GameData.Core.Interfaces;
using GameData.Core.Models;
using static GameData.Core.Constants.Spellbook;
using Functions = ForegroundBotRunner.Mem.Functions;

namespace ForegroundBotRunner.Objects
{
    public class LocalPlayer : WoWPlayer, IWoWLocalPlayer
    {
        internal LocalPlayer(nint pointer, HighGuid guid, WoWObjectType objectType)
            : base(pointer, guid, objectType) => RefreshSpells();

        private readonly Random random = new();

        // LUA SCRIPTS
        private const string WandLuaScript =
            "if IsCurrentAction(72) == nil then CastSpellByName('Shoot') end";
        private const string TurnOffWandLuaScript =
            "if IsCurrentAction(72) ~= nil then CastSpellByName('Shoot') end";
        private const string AutoAttackLuaScript =
            "if IsCurrentAction(72) == nil then CastSpellByName('Attack') end";
        private const string TurnOffAutoAttackLuaScript =
            "if IsCurrentAction(72) ~= nil then CastSpellByName('Attack') end";

        // OPCODES
        private const int SET_FACING_OPCODE = 0xDA;

        public readonly IDictionary<string, int[]> PlayerSpells = new Dictionary<string, int[]>();
        public readonly List<int> PlayerSkills = [];
        public new ulong TargetGuid => MemoryManager.ReadUlong(Offsets.Player.TargetGuid, true);

        public bool TargetInMeleeRange =>
            Functions.LuaCallWithResult("{0} = CheckInteractDistance(\"target\", 3)")[0] == "1";

        public new Class Class => (Class)MemoryManager.ReadByte(MemoryAddresses.LocalPlayerClass);
        public new Race Race =>
            Enum.GetValues(typeof(Race))
                .Cast<Race>()
                .FirstOrDefault(v =>
                    v.GetDescription() == Functions.LuaCallWithResult("{0} = UnitRace('player')")[0]
                );

        public Position CorpsePosition =>
            new(
                MemoryManager.ReadFloat(MemoryAddresses.LocalPlayerCorpsePositionX),
                MemoryManager.ReadFloat(MemoryAddresses.LocalPlayerCorpsePositionY),
                MemoryManager.ReadFloat(MemoryAddresses.LocalPlayerCorpsePositionZ)
            );

        public void Face(Position pos)
        {
            if (pos == null)
                return;

            // sometimes the client gets in a weird state and CurrentFacing is negative. correct that here.
            if (Facing < 0)
            {
                SetFacing((float)(Math.PI * 2) + Facing);
                return;
            }
            SetFacing(GetFacingForPosition(pos));
            return;
            //if this is a new position, restart the turning flow
            //if (turning && pos != turningToward)
            //{
            //    ResetFacingState();
            //    return;
            //}

            //// return if we're already facing the position
            //if (!turning && IsFacing(pos))
            //    return;

            //if (!turning)
            //{
            //    var requiredFacing = GetFacingForPosition(pos);
            //    float amountToTurn;
            //    if (requiredFacing > Facing)
            //    {
            //        if (requiredFacing - Facing > Math.PI)
            //        {
            //            amountToTurn = -((float)(Math.PI * 2) - requiredFacing + Facing);
            //        }
            //        else
            //        {
            //            amountToTurn = requiredFacing - Facing;
            //        }
            //    }
            //    else
            //    {
            //        if (Facing - requiredFacing > Math.PI)
            //        {
            //            amountToTurn = (float)(Math.PI * 2) - Facing + requiredFacing;
            //        }
            //        else
            //        {
            //            amountToTurn = requiredFacing - Facing;
            //        }
            //    }

            //    // if the turn amount is relatively small, just face that direction immediately
            //    if (Math.Abs(amountToTurn) < 0.05)
            //    {
            //        SetFacing(requiredFacing);
            //        ResetFacingState();
            //        return;
            //    }

            //    turning = true;
            //    turningToward = pos;
            //    amountPerTurn = amountToTurn / 2;
            //}
            //if (turning)
            //{
            //    if (turnCount < 1)
            //    {
            //        var twoPi = (float)(Math.PI * 2);
            //        var newFacing = Facing + amountPerTurn;

            //        if (newFacing < 0)
            //            newFacing = twoPi + amountPerTurn + Facing;
            //        else if (newFacing > Math.PI * 2)
            //            newFacing = amountPerTurn - (twoPi - Facing);

            //        SetFacing(newFacing);
            //        turnCount++;
            //    }
            //    else
            //    {
            //        SetFacing(GetFacingForPosition(pos));
            //        ResetFacingState();
            //    }
            //}
        }

        // Nat added this to see if he could test out the cleave radius which is larger than that isFacing radius
        public bool IsInCleave(Position position) =>
            Math.Abs(GetFacingForPosition(position) - Facing) < 3f;

        public void SetFacing(float facing)
        {
            Functions.SetFacing(
                nint.Add(Pointer, MemoryAddresses.LocalPlayer_SetFacingOffset),
                facing
            );
            Functions.SendMovementUpdate(Pointer, SET_FACING_OPCODE);
        }

        public void MoveToward(Position pos)
        {
            Face(pos);
            StartMovement(ControlBits.Front);
        }

        public void Turn180()
        {
            var newFacing = Facing + Math.PI;
            if (newFacing > Math.PI * 2)
                newFacing -= Math.PI * 2;
            SetFacing((float)newFacing);
        }

        // the client will NOT send a packet to the server if a key is already pressed, so you're safe to spam this
        public void StartMovement(ControlBits bits)
        {
            if (bits == ControlBits.Nothing)
                return;

            Functions.SetControlBit((int)bits, 1, Environment.TickCount);
        }

        public void StopAllMovement()
        {
            if (MovementFlags != MovementFlags.MOVEFLAG_NONE)
            {
                var bits =
                    ControlBits.Front
                    | ControlBits.Back
                    | ControlBits.Left
                    | ControlBits.Right
                    | ControlBits.StrafeLeft
                    | ControlBits.StrafeRight;

                StopMovement(bits);
            }
        }

        public void StopMovement(ControlBits bits)
        {
            if (bits == ControlBits.Nothing)
                return;

            Functions.SetControlBit((int)bits, 0, Environment.TickCount);
        }

        public void Jump()
        {
            StopMovement(ControlBits.Jump);
            StartMovement(ControlBits.Jump);
        }

        public void Stand() => Functions.LuaCall("DoEmote(\"STAND\")");

        public string CurrentStance
        {
            get
            {
                if (Buffs.Any(b => b.Name == BattleStance))
                    return BattleStance;

                if (Buffs.Any(b => b.Name == DefensiveStance))
                    return DefensiveStance;

                if (Buffs.Any(b => b.Name == BerserkerStance))
                    return BerserkerStance;

                return "None";
            }
        }

        public bool InGhostForm
        {
            get
            {
                var result = Functions.LuaCallWithResult("{0} = UnitIsGhost('player')");

                if (result.Length > 0)
                    return result[0] == "1";
                else
                    return false;
            }
        }

        public void SetTarget(ulong guid)
        {
            Functions.SetTarget(guid);
        }

        private ulong ComboPointGuid { get; set; }

        public int ComboPoints
        {
            get
            {
                var result = Functions.LuaCallWithResult("{0} = GetComboPoints('target')");

                if (result.Length > 0)
                    return Convert.ToByte(result[0]);
                else
                    return 0;
            }
        }

        public string CurrentShapeshiftForm
        {
            get
            {
                if (HasBuff(BearForm))
                    return BearForm;

                if (HasBuff(CatForm))
                    return CatForm;

                return "Human Form";
            }
        }

        public bool IsDiseased =>
            GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Disease);

        public bool IsCursed => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Curse);

        public bool IsPoisoned =>
            GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Poison);

        public bool HasMagicDebuff =>
            GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Magic);

        public void ReleaseCorpse() => Functions.ReleaseCorpse(Pointer);

        public void RetrieveCorpse() => Functions.RetrieveCorpse();

        public void RefreshSpells()
        {
            PlayerSpells.Clear();
            for (var i = 0; i < 1024; i++)
            {
                var currentSpellId = MemoryManager.ReadInt(
                    MemoryAddresses.LocalPlayerSpellsBase + 4 * i
                );
                if (currentSpellId == 0)
                    break;

                string name;
                var spellsBasePtr = MemoryManager.ReadIntPtr(0x00C0D788);
                var spellPtr = MemoryManager.ReadIntPtr(spellsBasePtr + currentSpellId * 4);

                var spellNamePtr = MemoryManager.ReadIntPtr(spellPtr + 0x1E0);
                name = MemoryManager.ReadString(spellNamePtr);

                if (PlayerSpells.TryGetValue(name, out int[]? value))
                    PlayerSpells[name] = [.. value, currentSpellId];
                else
                    PlayerSpells.Add(name, [currentSpellId]);
            }
        }

        public void RefreshSkills()
        {
            PlayerSkills.Clear();
            var skillPtr1 = MemoryManager.ReadIntPtr(nint.Add(Pointer, 8));
            var skillPtr2 = nint.Add(skillPtr1, 0xB38);

            var maxSkills = MemoryManager.ReadInt(0x00B700B4);
            for (var i = 0; i < maxSkills + 12; i++)
            {
                var curPointer = nint.Add(skillPtr2, i * 12);

                var id = (Skills)MemoryManager.ReadShort(curPointer);
                if (!Enum.IsDefined(typeof(Skills), id))
                {
                    continue;
                }

                PlayerSkills.Add((short)id);
            }
        }

        public int GetSpellId(string spellName, int rank = -1)
        {
            int spellId;

            var maxRank = PlayerSpells[spellName].Length;
            if (rank < 1 || rank > maxRank)
                spellId = PlayerSpells[spellName][maxRank - 1];
            else
                spellId = PlayerSpells[spellName][rank - 1];

            return spellId;
        }

        public bool IsSpellReady(string spellName, int rank = -1)
        {
            if (!PlayerSpells.ContainsKey(spellName))
                return false;

            var spellId = GetSpellId(spellName, rank);

            return !Functions.IsSpellOnCooldown(spellId);
        }

        public int GetManaCost(string spellName, int rank = -1)
        {
            var parId = GetSpellId(spellName, rank);

            if (parId >= MemoryManager.ReadUint(0x00C0D780 + 0xC) || parId <= 0)
                return 0;

            var entryPtr = MemoryManager.ReadIntPtr(
                (nint)(uint)(MemoryManager.ReadUint(0x00C0D780 + 8) + parId * 4)
            );
            return MemoryManager.ReadInt(entryPtr + 0x0080);
        }

        public bool KnowsSpell(string name) => PlayerSpells.ContainsKey(name);

        public bool MainhandIsEnchanted =>
            Functions.LuaCallWithResult("{0} = GetWeaponEnchantInfo()")[0] == "1";

        public ulong GetBackpackItemGuid(int slot) =>
            MemoryManager.ReadUlong(
                GetDescriptorPtr()
                    + (MemoryAddresses.LocalPlayer_BackpackFirstItemOffset + slot * 8)
            );

        public ulong GetEquippedItemGuid(EquipSlot slot) =>
            MemoryManager.ReadUlong(
                nint.Add(
                    Pointer,
                    MemoryAddresses.LocalPlayer_EquipmentFirstItemOffset + ((int)slot - 1) * 0x8
                )
            );

        public bool CanRiposte
        {
            get
            {
                if (PlayerSpells.ContainsKey("Riposte"))
                {
                    var results = Functions.LuaCallWithResult(
                        "{0}, {1} = IsUsableSpell('Riposte')"
                    );
                    if (results.Length > 0)
                        return results[0] == "1";
                    else
                        return false;
                }
                return false;
            }
        }

        public bool TastyCorpsesNearby => throw new NotImplementedException();
        public uint Copper => throw new NotImplementedException();
        public bool IsAutoAttacking => throw new NotImplementedException();
        public bool CanResurrect => throw new NotImplementedException();
        public bool InBattleground => throw new NotImplementedException();
        public bool HasQuestTargets => throw new NotImplementedException();
        public bool IsInWorld => throw new NotImplementedException();
        public bool HealthRestored => throw new NotImplementedException();

        public void StartMeleeAttack()
        {
            if (
                !IsCasting
                && (Class == Class.Warlock || Class == Class.Mage || Class == Class.Priest)
            )
            {
                Functions.LuaCall(WandLuaScript);
            }
            else if (Class != Class.Hunter)
            {
                Functions.LuaCall(AutoAttackLuaScript);
            }
        }

        public void DoEmote(Emote emote)
        {
            throw new NotImplementedException();
        }

        public void DoEmote(TextEmote emote)
        {
            throw new NotImplementedException();
        }

        public uint GetManaCost(string healingTouch)
        {
            throw new NotImplementedException();
        }

        public void StartRangedAttack()
        {
            throw new NotImplementedException();
        }

        public void StopAttack()
        {
            throw new NotImplementedException();
        }

        public bool IsSpellReady(string spellName)
        {
            throw new NotImplementedException();
        }

        public void CastSpell(string spellName, int rank = -1, bool castOnSelf = false)
        {
            throw new NotImplementedException();
        }

        public void CastSpell(uint spellId, int rank = -1, bool castOnSelf = false)
        {
            throw new NotImplementedException();
        }

        public void StartWandAttack()
        {
            throw new NotImplementedException();
        }

        public void MoveToward(Position position, float facing)
        {
            throw new NotImplementedException();
        }

        public void StopCasting()
        {
            throw new NotImplementedException();
        }

        public void CastSpell(int spellId, int rank = -1, bool castOnSelf = false)
        {
            throw new NotImplementedException();
        }

        public bool CanCastSpell(int spellId, ulong targetGuid)
        {
            throw new NotImplementedException();
        }

        public void UseItem(int bagId, int slotId, ulong targetGuid = 0)
        {
            throw new NotImplementedException();
        }

        public IWoWItem GetEquippedItem(EquipSlot ranged)
        {
            throw new NotImplementedException();
        }

        public IWoWItem GetContainedItem(int bagSlot, int slotId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWoWItem> GetEquippedItems()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWoWItem> GetContainedItems()
        {
            throw new NotImplementedException();
        }

        public uint GetBagGuid(EquipSlot equipSlot)
        {
            throw new NotImplementedException();
        }

        public void PickupContainedItem(int bagSlot, int slotId, int quantity)
        {
            throw new NotImplementedException();
        }

        public void PlaceItemInContainer(int bagSlot, int slotId)
        {
            throw new NotImplementedException();
        }

        public void DestroyItemInContainer(int bagSlot, int slotId, int quantity = -1)
        {
            throw new NotImplementedException();
        }

        public void Logout()
        {
            throw new NotImplementedException();
        }

        public void SplitStack(
            int bag,
            int slot,
            int quantity,
            int destinationBag,
            int destinationSlot
        )
        {
            throw new NotImplementedException();
        }

        public void EquipItem(int bagSlot, int slotId, EquipSlot? equipSlot = null)
        {
            throw new NotImplementedException();
        }

        public void UnequipItem(EquipSlot slot)
        {
            throw new NotImplementedException();
        }

        public void AcceptResurrect()
        {
            throw new NotImplementedException();
        }
    }
}
