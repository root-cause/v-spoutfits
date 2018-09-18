using System;
using System.Xml.Serialization;
using GTA;
using GTA.Native;

namespace SPOutfits
{
    [Serializable]
    public class Outfit
    {
        public string Name { get; set; }
        public Character Character { get; set; }

        // Drawables (clothing)
        public int[] Drawables { get; set; } = new int[Constants.MaxDrawables];
        public int[] DrawableTextures { get; set; } = new int[Constants.MaxDrawables];
        public int[] DrawablePalettes { get; set; } = new int[Constants.MaxDrawables];

        // Props (hats, glasses etc.)
        public int[] Props { get; set; } = new int[Constants.MaxProps];
        public int[] PropTextures { get; set; } = new int[Constants.MaxProps];

        [XmlIgnore]
        public string FilePath { get; set; }

        public void Apply()
        {
            int pedHandle = Game.Player.Character.Handle;

            // Apply drawables
            for (int i = 0; i < Constants.MaxDrawables; i++) Function.Call(Hash.SET_PED_COMPONENT_VARIATION, pedHandle, i, Drawables[i], DrawableTextures[i], DrawablePalettes[i]);

            // Apply props
            Function.Call(Hash.CLEAR_ALL_PED_PROPS, pedHandle);
            for (int i = 0; i < Constants.MaxProps; i++) Function.Call(Hash.SET_PED_PROP_INDEX, pedHandle, i, Props[i], PropTextures[i], true);
        }
    }
}
