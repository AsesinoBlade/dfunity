// Project:      Enemy Loot, The Penwick Papers for Daggerfall Unity
// Author:       DunnyOfPenwick
// Origin Date:  June 2022

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;

namespace ThePenwickPapers
{



    public class Loot
    {

        public static void AddItems(GameObject enemy)
        {
            if (enemy.name.Contains("Penwick"))
                return; //summoned creatures don't need loot adjustment

            DaggerfallEntityBehaviour target = enemy.GetComponent<DaggerfallEntityBehaviour>();

            if (target == null || target.Entity == null)
                return;

            if (!(target.Entity is EnemyEntity))
                return;

            EnemyEntity entity = (EnemyEntity)target.Entity;

            switch ((MobileTypes)entity.MobileEnemy.ID)
            {
                case MobileTypes.Assassin:
                case MobileTypes.Burglar:
                    AddPotionOfSeekingItems(entity, 25);
                    break;
                case MobileTypes.Thief:
                    AddPotionOfSeekingItems(entity, 10);
                    break;
                case MobileTypes.GiantScorpion:
                    AddItem(entity, 35, ItemGroups.CreatureIngredients2, (int)CreatureIngredients2.Giant_scorpion_stinger);
                    break;
                case MobileTypes.Spider:
                    AddItem(entity, 30, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Spider_venom);
                    break;
                case MobileTypes.Mummy:
                    AddItem(entity, 40, ItemGroups.CreatureIngredients2, (int)CreatureIngredients2.Mummy_wrappings);
                    break;
                case MobileTypes.Barbarian:
                case MobileTypes.Healer:
                    AddItem(entity, 28, ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Elixir_vitae);
                    AddItem(entity, 24, ItemGroups.PlantIngredients2, (int)PlantIngredients2.Aloe);
                    AddItem(entity, 27, ItemGroups.PlantIngredients1, (int)PlantIngredients1.Root_bulb);
                    AddItem(entity, 18 + entity.Level, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Troll_blood);
                    break;
                case MobileTypes.Mage:
                case MobileTypes.Sorcerer:
                    AddItem(entity, 27, ItemGroups.MetalIngredients, (int)MetalIngredients.Silver);
                    AddItem(entity, 25, ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Nectar);
                    AddItem(entity, 25, ItemGroups.Gems, (int)Gems.Amber);
                    AddItem(entity, 14 + entity.Level, ItemGroups.CreatureIngredients2, (int)CreatureIngredients2.Dragons_scales);
                    AddSoulTrap(entity, 3 + entity.Level);
                    break;
                case MobileTypes.Bard:
                case MobileTypes.Battlemage:
                case MobileTypes.Spellsword:
                    AddSoulTrap(entity, entity.Level - 6);
                    break;
                case MobileTypes.Orc:
                case MobileTypes.OrcSergeant:
                case MobileTypes.OrcWarlord:
                    AddItem(entity, 30, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Orcs_blood);
                    break;
                case MobileTypes.OrcShaman:
                    AddItem(entity, 30, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Orcs_blood);
                    AddSoulTrap(entity, 10);
                    break;
                case MobileTypes.Lich:
                    AddItem(entity, 30, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Lich_dust);
                    AddSoulTrap(entity, 15);
                    break;
                case MobileTypes.AncientLich:
                case MobileTypes.VampireAncient:
                case MobileTypes.DaedraLord:
                    AddSoulTrap(entity, 22);
                    break;
                case MobileTypes.SabertoothTiger:
                    AddItem(entity, 40, ItemGroups.MiscellaneousIngredients1, (int)MiscellaneousIngredients1.Big_tooth);
                    break;
                case MobileTypes.Nymph:
                    AddItem(entity, 40, ItemGroups.CreatureIngredients3, (int)CreatureIngredients3.Nymph_hair);
                    break;
                case MobileTypes.Wereboar:
                    AddItem(entity, 40, ItemGroups.CreatureIngredients3, (int)CreatureIngredients3.Wereboar_tusk);
                    break;
                case MobileTypes.Werewolf:
                    AddItem(entity, 25, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Werewolfs_blood);
                    break;
                case MobileTypes.Ghost:
                    AddItem(entity, 35, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Ectoplasm);
                    break;
                case MobileTypes.Wraith:
                    AddItem(entity, 35, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Wraith_essence);
                    break;
                case MobileTypes.Giant:
                    AddItem(entity, 40, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Giant_blood);
                    break;

                default:
                    break;
            }

        }


        private static void AddItem(EnemyEntity entity, int chance, ItemGroups group, int templateIndex)
        {
            if (Dice100.FailedRoll(chance))
                return;

            DaggerfallUnityItem item = ItemBuilder.CreateItem(group, templateIndex);
            entity.Items.AddItem(item);
        }


        private static void AddSoulTrap(EnemyEntity entity, int chance)
        {
            if (Dice100.FailedRoll(chance))
                return;

            DaggerfallUnityItem item = ItemBuilder.CreateRandomlyFilledSoulTrap();
            int soulStrength = Reanimate.GetSoulValue(item.TrappedSoulType);

            if (Dice100.SuccessRoll(70) || soulStrength > 100 || soulStrength > 5 * entity.Level)
            {
                item.TrappedSoulType = MobileTypes.None;
            }

            entity.Items.AddItem(item);
        }


        private static void AddPotionOfSeekingItems(EnemyEntity entity, int baseChance)
        {
            int potionOfSeekingKey = ThePenwickPapersMod.Instance.GetPotionOfSeekingRecipeKey();

            if (Dice100.SuccessRoll(baseChance / 2))
            {
                DaggerfallUnityItem potionRecipe = new DaggerfallUnityItem(ItemGroups.MiscItems, 4) { PotionRecipeKey = potionOfSeekingKey };
                entity.Items.AddItem(potionRecipe);
            }
            if (Dice100.SuccessRoll(baseChance))
            {
                DaggerfallUnityItem potion = ItemBuilder.CreatePotion(potionOfSeekingKey);
                entity.Items.AddItem(potion);
            }

            AddItem(entity, baseChance, ItemGroups.PlantIngredients1, (int)PlantIngredients1.Root_tendrils);
            AddItem(entity, baseChance, ItemGroups.PlantIngredients2, (int)PlantIngredients2.Black_poppy);
            AddItem(entity, baseChance, ItemGroups.MetalIngredients, (int)MetalIngredients.Lodestone);
            AddItem(entity, baseChance, ItemGroups.CreatureIngredients1, (int)CreatureIngredients1.Basilisk_eye);
        }



    } //class EnemyLoot



} //namespace
