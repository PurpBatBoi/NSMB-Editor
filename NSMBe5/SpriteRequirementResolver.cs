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
    internal static class SpriteRequirementResolver
    {
        private const int SpriteBankBlock = 13;

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
    }
}
