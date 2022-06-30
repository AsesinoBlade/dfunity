// Project:     Skill Advancement, The Penwick Papers for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: Mar 2022

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;


namespace ThePenwickPapers
{

    public class SkillAdvancement
    {
        private static float lastAdvancementTime;
        private static int skillIndex;

        private static readonly Dictionary<DFCareer.Skills, double> adjustments = new Dictionary<DFCareer.Skills, double>()
        {
            { DFCareer.Skills.Alteration, 0.60 },
            { DFCareer.Skills.Archery, 0.90 },
            { DFCareer.Skills.Axe, 1.15 },
            { DFCareer.Skills.Backstabbing, 0.70 },
            { DFCareer.Skills.BluntWeapon, 1.15 },
            { DFCareer.Skills.Centaurian, 0.50 },
            { DFCareer.Skills.Climbing, 1.00 },
            { DFCareer.Skills.CriticalStrike, 0.70 },
            { DFCareer.Skills.Daedric, 0.50 },
            { DFCareer.Skills.Destruction, 1.10 },
            { DFCareer.Skills.Dodging, 0.75 },
            { DFCareer.Skills.Dragonish, 0.50 },
            { DFCareer.Skills.Etiquette, 0.80 },
            { DFCareer.Skills.Giantish, 0.50 },
            { DFCareer.Skills.HandToHand, 1.15 },
            { DFCareer.Skills.Harpy, 0.50 },
            { DFCareer.Skills.Illusion, 0.60 },
            { DFCareer.Skills.Impish, 0.50 },
            { DFCareer.Skills.Jumping, 0.80 },
            { DFCareer.Skills.Lockpicking, 0.50 },
            { DFCareer.Skills.LongBlade, 1.15 },
            { DFCareer.Skills.Medical, 1.00 },
            { DFCareer.Skills.Mercantile, 1.00 },
            { DFCareer.Skills.Mysticism, 0.60 },
            { DFCareer.Skills.Nymph, 0.50 },
            { DFCareer.Skills.Orcish, 0.50 },
            { DFCareer.Skills.Pickpocket, 0.80 },
            { DFCareer.Skills.Restoration, 0.80 },
            { DFCareer.Skills.Running, 1.00 },
            { DFCareer.Skills.ShortBlade, 1.25 },
            { DFCareer.Skills.Spriggan, 0.50 },
            { DFCareer.Skills.Stealth, 1.00 },
            { DFCareer.Skills.Streetwise, 0.80 },
            { DFCareer.Skills.Swimming, 0.50 },
            { DFCareer.Skills.Thaumaturgy, 0.60 },
        };

        public static int SkillPerLevel = 15;


        /// <summary>
        /// FormulaHelper override to include governing stat when calculating skill advancement.
        /// </summary>
        public static int CalculateSkillUsesForAdvancement(int skillValue, int skillAdvancementMultiplier, float careerAdvancementMultiplier, int level)
        {
            DaggerfallEntity player = GameManager.Instance.PlayerEntity;

            double governingAttributeMultiplier = 1.0;
            double skillCorrectionMultiplier = 1.0;
            double exceedingGoverningAttributeMultiplier = 1.0;

            //Hack: we aren't being passed the skill, so we will infer it based on available information.

            //This function is only occasionally called from PlayerEntity after a long rest or travel.
            //It will be called once for each skill in a loop.
            //All calls should occur in the same frame, so checking frame time.
            if (Time.time != lastAdvancementTime)
            {
                lastAdvancementTime = Time.time;
                skillIndex = 0;
            }
            else
                ++skillIndex;


            DFCareer.Skills skill = (DFCareer.Skills)skillIndex;

            int value = player.Skills.GetPermanentSkillValue(skill);
            int advancementMult = DaggerfallSkills.GetAdvancementMultiplier(skill);

            //verify values match before changing anything
            if (value == skillValue && advancementMult == skillAdvancementMultiplier)
            {
                DFCareer.Stats stat = DaggerfallSkills.GetPrimaryStat(skill);
                float statValue = Mathf.Clamp(player.Stats.GetPermanentStatValue(stat), 10, 100);

                governingAttributeMultiplier = 1 / Mathf.Sqrt((statValue - 9f) / 50f);

                if (skillValue >= statValue)
                    exceedingGoverningAttributeMultiplier = 2.0;

                //some skill smoothing
                double dictValue;
                if (adjustments.TryGetValue((DFCareer.Skills)skillIndex, out dictValue))
                    skillCorrectionMultiplier = dictValue;
                else
                    Debug.LogWarningFormat("Penwick SkillAdvancement missing entry for skill# {0}", skillIndex);
            }

            //starting with the same basic formula used in FormulaHelper
            double levelMod = Math.Pow(1.04, level);
            double basic = (skillValue * skillAdvancementMultiplier * careerAdvancementMultiplier * levelMod * 2 / 5) + 1;

            double modAdjustment = governingAttributeMultiplier * skillCorrectionMultiplier * exceedingGoverningAttributeMultiplier;

            double total = basic * modAdjustment;

            return (int)Math.Floor(total);
        }


        /// <summary>
        /// FormulaHelper override to adjust character advancement rate.
        /// </summary>
        public static int CalculatePlayerLevel(int startingLevelUpSkillsSum, int currentLevelUpSkillsSum)
        {
            return (int)Mathf.Floor((currentLevelUpSkillsSum - startingLevelUpSkillsSum + 28) / SkillPerLevel);
        }


    } //class SkillAdvancement


} //namespace
