/**
 * MaNGOS is a full featured server for World of Warcraft, supporting
 * the following clients: 1.12.x, 2.4.3, 3.3.5a, 4.3.4a and 5.4.8
 *
 * Copyright (C) 2005-2025 MaNGOS <https://www.getmangos.eu>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 * World of Warcraft, and all World of Warcraft or Warcraft art, images,
 * and lore are copyrighted by Blizzard Entertainment, Inc.
 */

#ifndef MANGOS_H_WORLDMODEL
#define MANGOS_H_WORLDMODEL

#include "BIH.h"

namespace VMAP
{
    class TreeNode;
    struct AreaInfo;
    struct LocationInfo;

    /**
     * @brief Class representing a mesh triangle.
     */
    class MeshTriangle
    {
    public:
        /**
         * @brief Default constructor for MeshTriangle.
         */
        MeshTriangle() : idx0(0), idx1(0), idx2(0) {}
        /**
         * @brief Constructor for MeshTriangle with indices.
         *
         * @param na Index of the first vertex.
         * @param nb Index of the second vertex.
         * @param nc Index of the third vertex.
         */
        MeshTriangle(unsigned int na, unsigned int nb, unsigned int nc) : idx0(na), idx1(nb), idx2(nc) {}

        unsigned int idx0; /**< Index of the first vertex. */
        unsigned int idx1; /**< Index of the second vertex. */
        unsigned int idx2; /**< Index of the third vertex. */
    };

    /**
     * @brief Class representing WMO liquid data.
     */
    class WmoLiquid
    {
    public:
        /**
         * @brief Constructor for WmoLiquid.
         *
         * @param width Width of the liquid area.
         * @param height Height of the liquid area.
         * @param corner The lower corner of the liquid area.
         * @param type The type of the liquid.
         */
        WmoLiquid(unsigned int width, unsigned int height, const Vec3& corner, unsigned int type);
        /**
         * @brief Copy constructor for WmoLiquid.
         *
         * @param other The WmoLiquid to copy from.
         */
        WmoLiquid(const WmoLiquid& other);
        /**
         * @brief Destructor for WmoLiquid.
         */
        ~WmoLiquid();
        /**
         * @brief Assignment operator for WmoLiquid.
         *
         * @param other The WmoLiquid to assign from.
         * @return WmoLiquid& Reference to the assigned WmoLiquid.
         */
        WmoLiquid& operator=(const WmoLiquid& other);
        /**
         * @brief Gets the liquid height at a specific position.
         *
         * @param pos The position to check.
         * @param liqHeight The liquid height at the position.
         * @return bool True if the liquid height was retrieved, false otherwise.
         */
        bool GetLiquidHeight(const Vec3& pos, float& liqHeight) const;
        /**
         * @brief Gets the type of the liquid.
         *
         * @return unsigned int The type of the liquid.
         */
        unsigned int GetType() const { return iType; }
        /**
         * @brief Gets the height storage array.
         *
         * @return float* Pointer to the height storage array.
         */
        float* GetHeightStorage() { return iHeight; }
        /**
         * @brief Gets the flags storage array.
         *
         * @return uint8* Pointer to the flags storage array.
         */
        uint8_t* GetFlagsStorage() { return iFlags; }
        /**
         * @brief Gets the file size of the liquid data.
         *
         * @return unsigned int The file size of the liquid data.
         */
        unsigned int GetFileSize() const;
        /**
         * @brief Writes the liquid data to a file.
         *
         * @param wf The file to write to.
         * @return bool True if the write was successful, false otherwise.
         */
        bool WriteToFile(FILE* wf) const;
        /**
         * @brief Reads the liquid data from a file.
         *
         * @param rf The file to read from.
         * @param liquid The WmoLiquid to read into.
         * @return bool True if the read was successful, false otherwise.
         */
        static bool ReadFromFile(FILE* rf, WmoLiquid*& liquid);
    private:
        /**
         * @brief Default constructor for WmoLiquid.
         */
        WmoLiquid() : iTilesX(0), iTilesY(0), iCorner(Vec3::zero()), iType(0), iHeight(0), iFlags(0) {}

        unsigned int iTilesX;  /**< Number of tiles in x direction. */
        unsigned int iTilesY;  /**< Number of tiles in y direction. */
        Vec3 iCorner; /**< The lower corner of the liquid area. */
        unsigned int iType;    /**< The type of the liquid. */
        float* iHeight;  /**< Height values for the liquid area. (tilesX + 1)*(tilesY + 1) */
        uint8_t* iFlags;   /**< Flags indicating if a liquid tile is used. */
#ifdef MMAP_GENERATOR
    public:
        /**
         * @brief Gets the position information of the liquid.
         *
         * @param tilesX The number of tiles in x direction.
         * @param tilesY The number of tiles in y direction.
         * @param corner The lower corner of the liquid area.
         */
        void getPosInfo(unsigned int& tilesX, unsigned int& tilesY, Vec3& corner) const;
#endif
    };

    /**
     * @brief Class holding additional info for WMO group files.
     */
    class GroupModel
    {
    public:
        /**
         * @brief Default constructor for GroupModel.
         */
        GroupModel() : iMogpFlags(0), iGroupWMOID(0), iLiquid(0) {}
        /**
         * @brief Copy constructor for GroupModel.
         *
         * @param other The GroupModel to copy from.
         */
        GroupModel(const GroupModel& other);
        /**
         * @brief Constructor for GroupModel with parameters.
         *
         * @param mogpFlags Flags for the group model.
         * @param groupWMOID ID of the group WMO.
         * @param bound Bounding box of the group model.
         */
        GroupModel(unsigned int mogpFlags, unsigned int groupWMOID, const AABox& bound) :
            iBound(bound), iMogpFlags(mogpFlags), iGroupWMOID(groupWMOID), iLiquid(0) {
        }
        /**
         * @brief Destructor for GroupModel.
         */
        ~GroupModel() { delete iLiquid; }

        /**
         * @brief Pass mesh data to object and create BIH. Passed vectors get swapped with old geometry.
         *
         * @param vert Vector of vertices.
         * @param tri Vector of triangles.
         */
        void SetMeshData(std::vector<Vec3>& vert, std::vector<MeshTriangle>& tri);
        /**
         * @brief Sets the liquid data.
         *
         * @param liquid Pointer to the WmoLiquid.
         */
        void SetLiquidData(WmoLiquid*& liquid) { iLiquid = liquid; liquid = NULL; }
        /**
         * @brief Checks if a ray intersects with the group model.
         *
         * @param ray The ray to check.
         * @param distance The distance to the intersection.
         * @param stopAtFirstHit Whether to stop at the first hit.
         * @return bool True if the ray intersects, false otherwise.
         */
        bool IntersectRay(const Ray& ray, float& distance, bool stopAtFirstHit) const;
        /**
         * @brief Checks if a position is inside the object.
         *
         * @param pos The position to check.
         * @param down The direction vector.
         * @param z_dist The distance to the intersection.
         * @return bool True if the position is inside the object, false otherwise.
         */
        bool IsInsideObject(const Vec3& pos, const Vec3& down, float& z_dist) const;
        /**
         * @brief Gets the liquid level at a specific position.
         *
         * @param pos The position to check.
         * @param liqHeight The liquid height at the position.
         * @return bool True if the liquid level was retrieved, false otherwise.
         */
        bool GetLiquidLevel(const Vec3& pos, float& liqHeight) const;
        /**
         * @brief Gets the type of the liquid.
         *
         * @return unsigned int The type of the liquid.
         */
        unsigned int GetLiquidType() const;
        /**
         * @brief Writes the group model data to a file.
         *
         * @param wf The file to write to.
         * @return bool True if the write was successful, false otherwise.
         */
        bool WriteToFile(FILE* wf);
        /**
         * @brief Reads the group model data from a file.
         *
         * @param rf The file to read from.
         * @return bool True if the read was successful, false otherwise.
         */
        bool ReadFromFile(FILE* rf);
        /**
         * @brief Gets the bounding box of the group model.
         *
         * @return const G3D::AABox& The bounding box of the group model.
         */
        const AABox& GetBound() const { return iBound; }
        /**
         * @brief Gets the flags of the group model.
         *
         * @return unsigned int The flags of the group model.
         */
        unsigned int GetMogpFlags() const { return iMogpFlags; }
        /**
         * @brief Gets the ID of the group WMO.
         *
         * @return unsigned int The ID of the group WMO.
         */
        unsigned int GetWmoID() const { return iGroupWMOID; }
    protected:
        AABox iBound;  /**< Bounding box of the group model. */
        unsigned int iMogpFlags;  /**< Flags for the group model. */
        unsigned int iGroupWMOID; /**< ID of the group WMO. */
        std::vector<Vec3> vertices; /**< Vector of vertices. */
        std::vector<MeshTriangle> triangles; /**< Vector of triangles. */
        BIH meshTree; /**< Bounding Interval Hierarchy tree. */
        WmoLiquid* iLiquid; /**< Pointer to the WmoLiquid. */

#ifdef MMAP_GENERATOR
    public:
        /**
         * @brief Gets the mesh data of the group model.
         *
         * @param vertices Vector to store the vertices.
         * @param triangles Vector to store the triangles.
         * @param liquid Pointer to store the WmoLiquid.
         */
        void getMeshData(std::vector<Vec3>& vertices, std::vector<MeshTriangle>& triangles, WmoLiquid*& liquid);
#endif
    };

    /**
     * @brief Holds a model (converted M2 or WMO) in its original coordinate space.
     */
    class WorldModel
    {
    public:
        /**
         * @brief Default constructor for WorldModel.
         */
        WorldModel() : RootWMOID(0), Flags(0) {}

        /**
         * @brief Pass group models to WorldModel and create BIH. Passed vector is swapped with old geometry.
         *
         * @param models Vector of group models.
         */
        void SetGroupModels(std::vector<GroupModel>& models);
        /**
         * @brief Sets the root WMO ID.
         *
         * @param id The root WMO ID.
         */
        void SetRootWmoID(unsigned int id) { RootWMOID = id; }
        /**
         * @brief Checks if a ray intersects with the world model.
         *
         * @param ray The ray to check.
         * @param distance The distance to the intersection.
         * @param stopAtFirstHit Whether to stop at the first hit.
         * @return bool True if the ray intersects, false otherwise.
         */
        bool IntersectRay(const Ray& ray, float& distance, bool stopAtFirstHit) const;
        /**
         * @brief Gets area information at a specific position.
         *
         * @param point The position to check.
         * @param dir The direction vector.
         * @param dist The distance to the intersection.
         * @param info The area information.
         * @return bool True if area information was retrieved, false otherwise.
         */
        bool GetAreaInfo(const Vec3& point, const Vec3& dir, float& dist, AreaInfo& info) const;
        /**
         * @brief Gets location information at a specific position.
         *
         * @param point The position to check.
         * @param dir The direction vector.
         * @param dist The distance to the intersection.
         * @param info The location information.
         * @return bool True if location information was retrieved, false otherwise.
         */
        bool GetLocationInfo(const Vec3& point, const Vec3& dir, float& dist, LocationInfo& info) const;
        /**
         * @brief Gets the contact point at a specific position.
         *
         * @param point The position to check.
         * @param dir The direction vector.
         * @param dist The distance to the intersection.
         * @return bool True if a contact point was found, false otherwise.
         */
        bool GetContactPoint(const Vec3& point, const Vec3& dir, float& dist) const;
        /**
         * @brief Writes the world model data to a file.
         *
         * @param filename The file to write to.
         * @return bool True if the write was successful, false otherwise.
         */
        bool WriteFile(const std::string& filename);
        /**
         * @brief Reads the world model data from a file.
         *
         * @param filename The file to read from.
         * @return bool True if the read was successful, false otherwise.
         */
        bool ReadFile(const std::string& filename);
        unsigned int Flags; /**< Flags for the world model. */
    protected:
        unsigned int RootWMOID; /**< ID of the root WMO. */
        std::vector<GroupModel> groupModels; /**< Vector of group models. */
        BIH groupTree; /**< Bounding Interval Hierarchy tree. */

#ifdef MMAP_GENERATOR
    public:
        /**
         * @brief Gets the group models of the world model.
         *
         * @param groupModels Vector to store the group models.
         */
        void getGroupModels(std::vector<GroupModel>& groupModels);
#endif
    };
} // namespace VMAP

#endif // MANGOS_H_WORLDMODEL