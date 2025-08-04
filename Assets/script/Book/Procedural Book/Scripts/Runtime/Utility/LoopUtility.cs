using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class LoopUtility
    {
        public static int NextIndex(int index, int arrayLength)
        {
            index++;
            if (index == arrayLength) return 0;
            return index;
        }

        public static int PrevIndex(int index, int arrayLength)
        {
            if (index == 0) return arrayLength - 1;
            return index - 1;
        }

        public static int LoopIndex(int index, int arrayLength)
        {
            return index - Mathf.FloorToInt((float)index / arrayLength) * arrayLength;
        }
    }
}