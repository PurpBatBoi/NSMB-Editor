/*
*   This file is part of NSMB Editor 5.
*
*   NSMB Editor 5 is free software: you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*   (at your option) any later version.
*
*   NSMB Editor 5 is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public License
*   along with NSMB Editor 5.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace NSMBe5
{
    internal struct SpriteBankRequirement
    {
        public int Slot;
        public int Bank;

        public SpriteBankRequirement(int slot, int bank)
        {
            Slot = slot;
            Bank = bank;
        }
    }

    internal static class SpriteRequirementResolver
    {
        private const int SpriteBankBlock = 13;
        private static System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<SpriteBankRequirement>> objectBankRequirements;
        private static readonly System.Collections.Generic.List<SpriteBankRequirement> emptyRequirements = new System.Collections.Generic.List<SpriteBankRequirement>();

        public static bool TryGetRequiredSpriteBank(int spriteType, out int slot, out int bank)
        {
            slot = 0;
            bank = 0;

            if (spriteType < 0 || spriteType >= ROM.NativeStageObjCount)
                return false;

            byte[] modifierTable = ROM.GetInlineFile(ROM.Data.File_Modifiers);
            int idx = spriteType << 1;

            slot = modifierTable[idx];
            bank = modifierTable[idx + 1];

            return IsValidSpriteBankSlot(slot) && bank > 0;
        }

        public static bool IsSpriteBankSatisfied(NSMBLevel level, int spriteType)
        {
            if (!TryGetRequiredSpriteBank(spriteType, out int slot, out int bank))
                return true;

            if (level.Blocks[SpriteBankBlock].Length == 0 || level.Blocks[SpriteBankBlock].Length <= slot)
                return false;

            return level.Blocks[SpriteBankBlock][slot] == bank;
        }

        private static bool IsValidSpriteBankSlot(int slot)
        {
            return (slot >= 0 && slot <= 9) || slot == 15;
        }

        public static System.Collections.Generic.IReadOnlyList<SpriteBankRequirement> GetRequiredBanksForObjectId(int objectId)
        {
            EnsureObjectRequirementsLoaded();
            if (objectBankRequirements.TryGetValue(objectId, out System.Collections.Generic.List<SpriteBankRequirement> requirements))
                return requirements;

            return emptyRequirements;
        }

        public static bool IsAnyRequiredBankSatisfied(NSMBLevel level, int objectId)
        {
            System.Collections.Generic.IReadOnlyList<SpriteBankRequirement> requirements = GetRequiredBanksForObjectId(objectId);
            if (requirements.Count == 0)
                return false;

            if (level.Blocks[SpriteBankBlock].Length == 0)
                return false;

            for (int i = 0; i < requirements.Count; i++)
            {
                SpriteBankRequirement requirement = requirements[i];
                if (level.Blocks[SpriteBankBlock].Length <= requirement.Slot)
                    continue;

                if (level.Blocks[SpriteBankBlock][requirement.Slot] == requirement.Bank)
                    return true;
            }

            return false;
        }

        private static void EnsureObjectRequirementsLoaded()
        {
            if (objectBankRequirements != null)
                return;

            objectBankRequirements = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<SpriteBankRequirement>>();

            for (int spriteType = 0; spriteType < ROM.NativeStageObjCount; spriteType++)
            {
                if (!TryGetRequiredSpriteBank(spriteType, out int slot, out int bank))
                    continue;

                int objectId = ROM.GetObjIDFromTable(spriteType);
                if (!objectBankRequirements.TryGetValue(objectId, out System.Collections.Generic.List<SpriteBankRequirement> requirements))
                {
                    requirements = new System.Collections.Generic.List<SpriteBankRequirement>();
                    objectBankRequirements[objectId] = requirements;
                }

                bool exists = false;
                for (int i = 0; i < requirements.Count; i++)
                {
                    if (requirements[i].Slot == slot && requirements[i].Bank == bank)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    requirements.Add(new SpriteBankRequirement(slot, bank));
            }
        }
    }
}
