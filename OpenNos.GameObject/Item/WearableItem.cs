﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
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
 */

using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using System;
using System.Diagnostics;

namespace OpenNos.GameObject
{
    public class WearableItem : Item
    {
        #region Instantiation

        public WearableItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ref ItemInstance inv, bool delay = false, string[] packetsplit = null)
        {
            switch (Effect)
            {
                default:
                    short slot = inv.Slot;
                    InventoryType itemToWearType = inv.Type;

                    if (inv == null)
                    {
                        return;
                    }
                    if (ItemValidTime > 0 && inv.IsBound)
                    {
                        inv.ItemDeleteTime = DateTime.Now.AddSeconds(ItemValidTime);
                    }
                    if (!inv.IsBound)
                    {
                        if (!delay && ((EquipmentSlot == EquipmentType.Fairy && (MaxElementRate == 70 || MaxElementRate == 80)) || (EquipmentSlot == EquipmentType.CostumeHat || EquipmentSlot == EquipmentType.CostumeSuit || EquipmentSlot == EquipmentType.WeaponSkin)))
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)itemToWearType}^{slot}^1 {Language.Instance.GetMessageFromKey("ASK_BIND")}");
                            return;
                        }
                        else if (delay)
                        {
                            inv.BoundCharacterId = session.Character.CharacterId;
                        }
                    }

                    double timeSpanSinceLastSpUsage = (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds - session.Character.LastSp;

                    if (EquipmentSlot == EquipmentType.Sp && inv.Rare == -2)
                    {
                        session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_EQUIP_DESTROYED_SP"), 0));
                        return;
                    }

                    if (EquipmentSlot == EquipmentType.Sp && timeSpanSinceLastSpUsage <= session.Character.SpCooldown && session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, InventoryType.Specialist) != null)
                    {
                        session.SendPacket(session.Character.GenerateMsg(String.Format(Language.Instance.GetMessageFromKey("SP_INLOADING"), session.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage)), 0));
                        return;
                    }
                    if ((ItemType != ItemType.Weapon
                         && ItemType != ItemType.Armor
                         && ItemType != ItemType.Fashion
                         && ItemType != ItemType.Jewelery
                         && ItemType != ItemType.Specialist)
                        || LevelMinimum > (IsHeroic ? session.Character.HeroLevel : session.Character.Level) || (Sex != 0 && Sex != (byte)session.Character.Gender + 1)
                        || ((ItemType != ItemType.Jewelery && EquipmentSlot != EquipmentType.Boots && EquipmentSlot != EquipmentType.Gloves) && ((Class >> (byte)session.Character.Class) & 1) != 1))
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"), 10));
                        return;
                    }

                    if (session.Character.UseSp)
                    {
                        SpecialistInstance sp = session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, InventoryType.Wear);

                        if (sp.Item.Element != 0 && EquipmentSlot == EquipmentType.Fairy && Element != sp.Item.Element && Element != sp.Item.SecondaryElement)
                        {
                            session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("BAD_FAIRY"), 0));
                            return;
                        }
                    }

                    if (session.Character.UseSp && EquipmentSlot == EquipmentType.Sp)
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SP_BLOCKED"), 10));
                        return;
                    }

                    if (session.Character.JobLevel < LevelJobMinimum)
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOW_JOB_LVL"), 10));
                        return;
                    }

                    ItemInstance currentlyEquippedItem = session.Character.Inventory.LoadBySlotAndType((short)EquipmentSlot, InventoryType.Wear);
                    if (EquipmentSlot == EquipmentType.Amulet)
                    {
                        session.SendPacket(session.Character.GenerateEff(39));
                        inv.BoundCharacterId = session.Character.CharacterId;
                    }

                    if (currentlyEquippedItem == null)
                    {
                        // move from equipment to wear
                        session.Character.Inventory.MoveInInventory(inv.Slot, itemToWearType, InventoryType.Wear);
                        session.SendPacket(session.Character.GenerateInventoryAdd(-1, 0, itemToWearType, slot, 0, 0, 0, 0));
                        session.SendPacket(session.Character.GenerateStatChar());
                        session.CurrentMap?.Broadcast(session.Character.GenerateEq());
                        session.SendPacket(session.Character.GenerateEquipment());
                        session.CurrentMap?.Broadcast(session.Character.GeneratePairy());
                    }
                    else
                    {
                        // move from wear to equipment and back
                        session.Character.Inventory.MoveInInventory(currentlyEquippedItem.Slot, InventoryType.Wear, itemToWearType, inv.Slot);

                        session.SendPacket(session.Character.GenerateInventoryAdd(-1, 0, itemToWearType, slot, 0, 0, 0, 0));
                        session.SendPacket(session.Character.GenerateInventoryAdd(currentlyEquippedItem.ItemVNum, currentlyEquippedItem.Amount,
                            currentlyEquippedItem.Type, currentlyEquippedItem.Slot, currentlyEquippedItem.Rare, currentlyEquippedItem.Design, currentlyEquippedItem.Upgrade, (currentlyEquippedItem as SpecialistInstance)?.SpStoneUpgrade ?? (byte)0));

                        session.SendPacket(session.Character.GenerateStatChar());
                        session.CurrentMap?.Broadcast(session.Character.GenerateEq());
                        session.SendPacket(session.Character.GenerateEquipment());
                        session.CurrentMap?.Broadcast(session.Character.GeneratePairy());
                    }

                    if (EquipmentSlot == EquipmentType.Fairy)
                    {
                        WearableInstance fairy = session.Character.Inventory.LoadBySlotAndType<WearableInstance>((byte)EquipmentType.Fairy, InventoryType.Wear);
                        session.SendPacket(session.Character.GenerateSay(String.Format(Language.Instance.GetMessageFromKey("FAIRYSTATS"), fairy.XP, CharacterHelper.LoadFairyXPData(fairy.ElementRate + fairy.Item.ElementRate)), 10));
                    }
                    break;
            }
        }

        #endregion
    }
}