﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Commands;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using VRage.Groups;
using VRage.ObjectBuilders;
using VRageMath;

namespace CrunchEconomy
{

    //Class from LordTylus ALE Core
    //https://github.com/LordTylus/SE-Torch-ALE-Core/blob/master/GridManager.cs
    public static class GridManager
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static string newGridName;


        public static MyIdentity GetIdentityByNameOrId(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (ulong.TryParse(playerNameOrSteamId, out var steamId))
                {
                    var id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                        return identity;
                    if (identity.IdentityId == (long)steamId)
                        return identity;
                }

            }
            return null;
        }

        public static MyObjectBuilder_ShipBlueprintDefinition[] getBluePrint(string name, long newOwner, bool keepProjection, List<MyCubeGrid> grids)
        {
            var objectBuilders = new List<MyObjectBuilder_CubeGrid>();

            foreach (var grid in grids)
            {

                /* What else should it be? LOL? */
                if (!(grid.GetObjectBuilder(true) is MyObjectBuilder_CubeGrid objectBuilder))
                    throw new ArgumentException(grid + " has a ObjectBuilder thats not for a CubeGrid");

                objectBuilders.Add(objectBuilder);
            }

            var definition = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ShipBlueprintDefinition>();

            definition.Id = new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_ShipBlueprintDefinition)), name);
            definition.CubeGrids = objectBuilders.Select(x => (MyObjectBuilder_CubeGrid)x.Clone()).ToArray();

            /* Reset ownership as it will be different on the new server anyway */
            foreach (var cubeGrid in definition.CubeGrids)
            {
                cubeGrid.DisplayName = newOwner.ToString();

                foreach (var cubeBlock in cubeGrid.CubeBlocks)
                {
                    var ownerID = GetIdentityByNameOrId(newOwner.ToString()).IdentityId;
                    cubeBlock.Owner = ownerID;
                    cubeBlock.BuiltBy = ownerID;


                    /* Remove Projections if not needed */
                    if (!keepProjection)
                        if (cubeBlock is MyObjectBuilder_ProjectorBase projector)
                        {
                            projector.ProjectedGrid = null;
                            projector.ProjectedGrids = null;
                        }



                    /* Remove Pilot and Components (like Characters) from cockpits */
                    if (cubeBlock is MyObjectBuilder_Cockpit cockpit)
                    {

                        cockpit.Pilot = null;

                        if (cockpit.ComponentContainer != null)
                        {

                            var components = cockpit.ComponentContainer.Components;

                            if (components != null)
                            {

                                for (var i = components.Count - 1; i >= 0; i--)
                                {

                                    var component = components[i];

                                    if (component.TypeId == "MyHierarchyComponentBase")
                                    {
                                        components.RemoveAt(i);
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var builderDefinition = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
            builderDefinition.ShipBlueprints = new MyObjectBuilder_ShipBlueprintDefinition[] { definition };

            return builderDefinition.ShipBlueprints;
        }


        public static List<MyObjectBuilder_CubeGrid> GetObjectBuilders(string path)
        {
            if (MyObjectBuilderSerializer.DeserializeXML(path, out MyObjectBuilder_Definitions Definition))
            {
                var gridsToReturn = new List<MyObjectBuilder_CubeGrid>();
                if (Definition.Prefabs != null && Definition.Prefabs.Count() != 0)
                {
                    foreach (var prefab in Definition.Prefabs)
                    {
                        foreach (var grid in prefab.CubeGrids)
                        {
                            gridsToReturn.Add(grid);
                        }
                    }
                }
                else if (Definition.ShipBlueprints != null && Definition.ShipBlueprints.Count() != 0)
                {
                    foreach (var bp in Definition.ShipBlueprints)
                    {
                        foreach (var grid in bp.CubeGrids)
                        {
                            gridsToReturn.Add(grid);
                        }
                    }
                }
            }

            return null;
        }
        public static bool LoadGrid(string path, Vector3D playerPosition, bool keepOriginalLocation, ulong steamID, String name, bool force = false, CommandContext context = null)
        {
            if (MyObjectBuilderSerializer.DeserializeXML(path, out MyObjectBuilder_Definitions myObjectBuilder_Definitions))
            {

                var shipBlueprints = myObjectBuilder_Definitions.ShipBlueprints;

                if (shipBlueprints == null)
                {

                    Log.Warn("No ShipBlueprints in File '" + path + "'");

                    if (context != null)
                        context.Respond("There arent any Grids in your file to import!");

                    return false;
                }

                foreach (var shipBlueprint in shipBlueprints)
                {

                    if (!LoadShipBlueprint(shipBlueprint, playerPosition, keepOriginalLocation, (long)steamID, name, context, force))
                    {

                        Log.Warn("Error Loading ShipBlueprints from File '" + path + "'");
                        return false;
                    }
                }

                return true;
            }

            Log.Warn("Error Loading File '" + path + "' check Keen Logs.");

            return false;
        }



        public static bool LoadShipBlueprint(MyObjectBuilder_ShipBlueprintDefinition shipBlueprint,
            Vector3D playerPosition, bool keepOriginalLocation, long steamID, string Name, CommandContext context = null, bool force = false)
        {
            var grids = shipBlueprint.CubeGrids;

            if (grids == null || grids.Length == 0)
            {

                Log.Warn("No grids in blueprint!");

                if (context != null)
                    context.Respond("No grids in blueprint!");

                return false;
            }

            foreach (var grid in grids)
            {
                foreach (var block in grid.CubeBlocks)
                {
                    var ownerID = GetIdentityByNameOrId(steamID.ToString()).IdentityId;
                    block.Owner = ownerID;
                    block.BuiltBy = ownerID;
                }
            }

            var objectBuilderList = new List<MyObjectBuilder_EntityBase>(grids.ToList());

            if (!keepOriginalLocation)
            {

                /* Where do we want to paste the grids? Lets find out. */
                var pos = FindPastePosition(grids, playerPosition);
                if (pos == null)
                {

                    Log.Warn("No free Space found!");

                    if (context != null)
                        context.Respond("No free space available!");

                    return false;
                }

                var newPosition = pos.Value;

                /* Update GridsPosition if that doesnt work get out of here. */
                if (!UpdateGridsPosition(grids, newPosition))
                {

                    if (context != null)
                        context.Respond("The File to be imported does not seem to be compatible with the server!");

                    return false;
                }
                var player = MySession.Static.Players.GetPlayerByName(GetIdentityByNameOrId(steamID.ToString()).DisplayName).Character;
                var gps = CreateGps(pos.Value, Color.LightGreen, 60, Name);
                var gpsCollection = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                var gpsRef = gps;
                var entityId = 0L;
                entityId = gps.EntityId;
                gpsCollection.SendAddGpsRequest(player.GetPlayerIdentityId(), ref gpsRef, entityId, true);
            }
            else if (!force)
            {

                var sphere = FindBoundingSphere(grids);

                var position = grids[0].PositionAndOrientation.Value;

                sphere.Center = position.Position;

                var entities = new List<MyEntity>();
                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, entities);

                foreach (var entity in entities)
                {

                    if (entity is MyCubeGrid)
                    {

                        if (context != null)
                            context.Respond("There are potentially other grids in the way. If you are certain is free you can set 'force' to true!");

                        return false;
                    }
                }
            }
            /* Stop grids */
            foreach (var grid in grids)
            {

                grid.AngularVelocity = new SerializableVector3();
                grid.LinearVelocity = new SerializableVector3();

                var random = new Random();

            }
            /* Remapping to prevent any key problems upon paste. */
            MyEntities.RemapObjectBuilderCollection(objectBuilderList);

            var hasMultipleGrids = objectBuilderList.Count > 1;

            if (!hasMultipleGrids)
            {

                foreach (var ob in objectBuilderList)
                    MyEntities.CreateFromObjectBuilderParallel(ob, true);
            }
            else
            {
                MyEntities.Load(objectBuilderList, out _);
            }

            return true;
        }
        private static MyGps CreateGps(Vector3D Position, Color gpsColor, int seconds, string Name)
        {

            var gps = new MyGps
            {
                Coords = Position,
                Name = Name.Split('_')[0],
                DisplayName = Name.Split('_')[0] + " Paste Position",
                GPSColor = gpsColor,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = new TimeSpan(0, 0, seconds, 0),
                Description = "Paste Position",
            };
            gps.UpdateHash();


            return gps;
        }

        private static bool UpdateGridsPosition(MyObjectBuilder_CubeGrid[] grids, Vector3D newPosition)
        {

            var firstGrid = true;
            double deltaX = 0;
            double deltaY = 0;
            double deltaZ = 0;

            foreach (var grid in grids)
            {

                var position = grid.PositionAndOrientation;

                if (position == null)
                {

                    Log.Warn("Position and Orientation Information missing from Grid in file.");

                    return false;
                }

                var realPosition = position.Value;

                var currentPosition = realPosition.Position;

                if (firstGrid)
                {
                    deltaX = newPosition.X - currentPosition.X;
                    deltaY = newPosition.Y - currentPosition.Y;
                    deltaZ = newPosition.Z - currentPosition.Z;

                    currentPosition.X = newPosition.X;
                    currentPosition.Y = newPosition.Y;
                    currentPosition.Z = newPosition.Z;

                    firstGrid = false;

                }
                else
                {

                    currentPosition.X += deltaX;
                    currentPosition.Y += deltaY;
                    currentPosition.Z += deltaZ;
                }

                realPosition.Position = currentPosition;
                grid.PositionAndOrientation = realPosition;


            }

            return true;
        }

        private static Vector3D? FindPastePosition(MyObjectBuilder_CubeGrid[] grids, Vector3D playerPosition)
        {

            BoundingSphere sphere = FindBoundingSphere(grids);

            /* 
             * Now we know the radius that can house all grids which will now be 
             * used to determine the perfect place to paste the grids to. 
             */
            return MyEntities.FindFreePlace(playerPosition, sphere.Radius);
        }
        public static BoundingSphereD FindBoundingSphere(MyCubeGrid grid)
        {

            Vector3? vector = null;
            var radius = 0F;

            var obj = grid.GetObjectBuilder() as MyObjectBuilder_CubeGrid;
            var gridSphere = obj.CalculateBoundingSphere();

            /* If this is the first run, we use the center of that grid, and its radius as it is */
            if (vector == null)
            {

                vector = gridSphere.Center;
                radius = gridSphere.Radius;

            }
            else
            {

                /* 
                 * If its not the first run, we use the vector we already have and 
                 * figure out how far it is away from the center of the subgrids sphere. 
                 */
                var distance = Vector3.Distance(vector.Value, gridSphere.Center);

                /* 
                 * Now we figure out how big our new radius must be to house both grids
                 * so the distance between the center points + the radius of our subgrid.
                 */
                var newRadius = distance + gridSphere.Radius;

                /*
                 * If the new radius is bigger than our old one we use that, otherwise the subgrid 
                 * is contained in the other grid and therefore no need to make it bigger. 
                 */
                if (newRadius > radius)
                    radius = newRadius;
            }



            return new BoundingSphereD(vector.Value, radius);
        }
        private static BoundingSphereD FindBoundingSphere(MyObjectBuilder_CubeGrid[] grids)
        {

            Vector3? vector = null;
            var radius = 0F;

            foreach (var grid in grids)
            {

                var gridSphere = grid.CalculateBoundingSphere();

                /* If this is the first run, we use the center of that grid, and its radius as it is */
                if (vector == null)
                {

                    vector = gridSphere.Center;
                    radius = gridSphere.Radius;
                    continue;
                }

                /* 
                 * If its not the first run, we use the vector we already have and 
                 * figure out how far it is away from the center of the subgrids sphere. 
                 */
                var distance = Vector3.Distance(vector.Value, gridSphere.Center);

                /* 
                 * Now we figure out how big our new radius must be to house both grids
                 * so the distance between the center points + the radius of our subgrid.
                 */
                var newRadius = distance + gridSphere.Radius;

                /*
                 * If the new radius is bigger than our old one we use that, otherwise the subgrid 
                 * is contained in the other grid and therefore no need to make it bigger. 
                 */
                if (newRadius > radius)
                    radius = newRadius;


            }

            return new BoundingSphereD(vector.Value, radius);
        }
    }
}