﻿using OpenNos.Data;
using OpenNos.Domain;
using System;

namespace OpenNos.GameObject.Networking
{
    public class HitRequest : IEquatable<HitRequest>
    {
        #region Instantiation

        public HitRequest(TargetHitType targetHitType, ClientSession session, Skill skill, short? skillEffect = null, short? mapX = null, short? mapY = null, ComboDTO skillCombo = null)
        {
            HitTimestamp = DateTime.Now;
            Session = session;
            Skill = skill;
            TargetHitType = targetHitType;
            if (skillEffect.HasValue)
            {
                SkillEffect = skillEffect.Value;
            }
            else
            {
                SkillEffect = skill.Effect;
            }

            if (mapX.HasValue)
            {
                MapX = mapX.Value;
            }

            if (mapY.HasValue)
            {
                MapY = mapY.Value;
            }

            if (skillCombo != null)
            {
                SkillCombo = skillCombo;
            }
        }

        #endregion

        #region Properties

        public DateTime HitTimestamp { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public ClientSession Session { get; set; }

        public Skill Skill { get; set; }

        public ComboDTO SkillCombo { get; set; }

        public short SkillEffect { get; set; }

        public TargetHitType TargetHitType { get; set; }

        #endregion

        #region Methods

        public bool Equals(HitRequest other)
        {
            return other.HitTimestamp == HitTimestamp;
        }

        #endregion
    }
}