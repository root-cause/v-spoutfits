using GTA;
using GTA.Native;

namespace SPOutfits
{
    public static class Methods
    {
        public static Character GetCurrentCharacter()
        {
            switch ((PedHash)Game.Player.Character.Model.Hash)
            {
                case PedHash.Michael:
                    return Character.Michael;

                case PedHash.Franklin:
                    return Character.Franklin;

                case PedHash.Trevor:
                    return Character.Trevor;

                default:
                    return Character.Unknown;
            }
        }

        public static Outfit GetCurrentOutfit()
        {
            int pedHandle = Game.Player.Character.Handle;
            Outfit outfit = new Outfit();

            // Get drawables
            for (int i = 0; i < Constants.MaxDrawables; i++)
            {
                outfit.Drawables[i] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, pedHandle, i);
                outfit.DrawableTextures[i] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, pedHandle, i);
                outfit.DrawablePalettes[i] = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, pedHandle, i);
            }

            // Get props
            for (int i = 0; i < Constants.MaxProps; i++)
            {
                outfit.Props[i] = Function.Call<int>(Hash.GET_PED_PROP_INDEX, pedHandle, i);
                outfit.PropTextures[i] = Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, pedHandle, i);
            }

            return outfit;
        }
    }
}
