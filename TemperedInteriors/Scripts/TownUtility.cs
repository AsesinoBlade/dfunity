using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TemperedInteriors
{
    public class TownUtility
    {
        int maxQuality = 0;
        Vector2 maxPosition = Vector2.zero;


        public void ShowInfo()
        {
            if (maxQuality == 0)
            {
                GetBuildings();
            }

            ShowPlayerPositionInTown();

            DaggerfallUI.AddHUDText("Quality " + maxQuality + " house at " + maxPosition);
        }


        public void ShowPlayerPositionInTown()
        {
            Vector2 playerPos;
            if (!GameManager.Instance.IsPlayerInside)
            {
                float scale = MapsFile.WorldMapTerrainDim * MeshReader.GlobalScale;
                playerPos.x = ((GameManager.Instance.PlayerGPS.transform.position.x) % scale) / scale;
                playerPos.y = ((GameManager.Instance.PlayerGPS.transform.position.z) % scale) / scale;
                int refWidth = (int)(ExteriorAutomap.blockSizeWidth * ExteriorAutomap.numMaxBlocksX * GameManager.Instance.ExteriorAutomap.LayoutMultiplier);
                int refHeight = (int)(ExteriorAutomap.blockSizeHeight * ExteriorAutomap.numMaxBlocksY * GameManager.Instance.ExteriorAutomap.LayoutMultiplier);
                playerPos.x *= refWidth;
                playerPos.y *= refHeight;
                playerPos.x -= refWidth * 0.5f;
                playerPos.y -= refHeight * 0.5f;

                Debug.Log(playerPos);
            }
        }


        public void GetBuildings()
        {
            DFLocation location = GameManager.Instance.PlayerGPS.CurrentLocation;
            ExteriorAutomap.BlockLayout[] blockLayout = GameManager.Instance.ExteriorAutomap.ExteriorLayout;

            DFBlock[] blocks = RMBLayout.GetLocationBuildingData(location);
            int width = location.Exterior.ExteriorData.Width;
            int height = location.Exterior.ExteriorData.Height;

            int index = 0;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x, ++index)
                {
                    ref readonly DFBlock block = ref blocks[index];
                    BuildingSummary[] buildingsInBlock = RMBLayout.GetBuildingData(block, x, y);
                    for (int i = 0; i < buildingsInBlock.Length; ++i)
                    {
                        ref readonly BuildingSummary buildingSummary = ref buildingsInBlock[i];

                        // Compute building position in map coordinate system
                        float xPosBuilding = blockLayout[index].rect.xpos + (int)(buildingSummary.Position.x / (BlocksFile.RMBDimension * MeshReader.GlobalScale) * ExteriorAutomap.blockSizeWidth) - GameManager.Instance.ExteriorAutomap.LocationWidth * ExteriorAutomap.blockSizeWidth * 0.5f;
                        float yPosBuilding = blockLayout[index].rect.ypos + (int)(buildingSummary.Position.z / (BlocksFile.RMBDimension * MeshReader.GlobalScale) * ExteriorAutomap.blockSizeHeight) - GameManager.Instance.ExteriorAutomap.LocationHeight * ExteriorAutomap.blockSizeHeight * 0.5f;
                        Vector2 position = new Vector2(xPosBuilding, yPosBuilding);

                        switch (buildingSummary.BuildingType)
                        {
                            case DFLocation.BuildingTypes.House1:
                            case DFLocation.BuildingTypes.House2:
                            case DFLocation.BuildingTypes.House3:
                            case DFLocation.BuildingTypes.House4:
                                if (buildingSummary.Quality > maxQuality)
                                {
                                    maxQuality = buildingSummary.Quality;
                                    maxPosition = position;
                                }
                                break;
                            case DFLocation.BuildingTypes.House5: //garden hedges, city cemetary
                                break;
                            case DFLocation.BuildingTypes.House6: //towers, e.g. near Daggerfall palace entrance
                                break;
                            case DFLocation.BuildingTypes.Special1:
                            case DFLocation.BuildingTypes.Special2:
                            case DFLocation.BuildingTypes.Special3:
                            case DFLocation.BuildingTypes.Special4:
                                break; //???
                            case DFLocation.BuildingTypes.Town23: //town wall interiors
                                break;
                            case DFLocation.BuildingTypes.Town4: //??
                                break;
                        }

                    }
                }
            }

        }


    }



}
