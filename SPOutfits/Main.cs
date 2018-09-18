using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using GTA;
using NativeUI;

namespace SPOutfits
{
    public enum Character
    {
        Unknown = -1,
        Michael = 0,
        Franklin = 1,
        Trevor = 2
    }

    public class Main : Script
    {
        #region Settings
        Keys MenuKey = Keys.O;
        Keys DeleteKey = Keys.Delete;
        #endregion

        #region Variables
        List<Outfit> AllOutfits = new List<Outfit>();
        List<Outfit> CharacterOutfits = new List<Outfit>();

        MenuPool OutfitsMenuPool = null;
        UIMenu OutfitsMainMenu = null;
        UIMenu OutfitsListMenu = null;

        Character LastCharacter = Character.Unknown;
        Outfit TempOutfit = null;
        #endregion

        #region Event: Init
        public Main()
        {
            // Load settings
            try
            {
                string configFile = Path.Combine("scripts", "spoutfit_config.ini");
                ScriptSettings config = ScriptSettings.Load(configFile);

                if (File.Exists(configFile))
                {
                    MenuKey = config.GetValue("KEYS", "Menu", Keys.O);
                    DeleteKey = config.GetValue("KEYS", "Delete", Keys.Delete);
                }
                else
                {
                    config.SetValue("KEYS", "Menu", MenuKey);
                    config.SetValue("KEYS", "Delete", DeleteKey);
                }

                config.Save();
            }
            catch (Exception e)
            {
                UI.Notify($"~r~SPOutfit settings error: {e.Message}");
            }

            // Load outfits
            try
            {
                string outfitsPath = Path.Combine("scripts", "outfits");
                if (!Directory.Exists(outfitsPath)) Directory.CreateDirectory(outfitsPath);

                string[] files = Directory.GetFiles(outfitsPath, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    try
                    {
                        Outfit outfit = XmlUtil.Deserialize<Outfit>(File.ReadAllText(file));
                        outfit.FilePath = file;

                        AllOutfits.Add(outfit);
                    }
                    catch (Exception e)
                    {
                        UI.Notify($"~r~Outfit reading error: {e.Message} ({file})");
                    }
                }
            }
            catch (Exception e)
            {
                UI.Notify($"~r~Outfit loading error: {e.Message}");
            }

            // Set up menus
            OutfitsMenuPool = new MenuPool();
            OutfitsMainMenu = new UIMenu("", "OUTFITS MENU");
            OutfitsListMenu = new UIMenu("", "SAVED OUTFITS");

            UIMenuItem linkItem = new UIMenuItem("List", "See all of your saved outfits for the current character.");
            UIMenuItem saveItem = new UIMenuItem("Save", "Save your current outfit.");
            OutfitsMainMenu.AddItem(linkItem);
            OutfitsMainMenu.AddItem(saveItem);

            OutfitsMainMenu.BindMenuToItem(OutfitsListMenu, linkItem);
            OutfitsMenuPool.Add(OutfitsMainMenu);
            OutfitsMenuPool.Add(OutfitsListMenu);

            // Handle save item being selected
            saveItem.Activated += (menu, item) =>
            {
                string outfitName = Game.GetUserInput(WindowTitle.FMMC_KEY_TIP9N, 16).Trim();
                if (outfitName.Length < 1)
                {
                    UI.Notify("~r~You didn't write a name!");
                    return;
                }

                if (outfitName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                {
                    UI.Notify("~r~Your outfit name contains not allowed characters.");
                    return;
                }

                Character currentCharacter = Methods.GetCurrentCharacter();
                if (CharacterOutfits.FirstOrDefault(o => o.Name == outfitName) != null)
                {
                    UI.Notify($"~r~Outfit \"{outfitName}\" already exists for {currentCharacter}!");
                    return;
                }

                try
                {
                    string path = Path.Combine("scripts", "outfits", $"{currentCharacter}_{outfitName}.xml");
                    Outfit newOutfit = Methods.GetCurrentOutfit();
                    newOutfit.Name = outfitName;
                    newOutfit.Character = currentCharacter;
                    newOutfit.FilePath = path;

                    AllOutfits.Add(newOutfit);
                    CharacterOutfits.Add(newOutfit);
                    CharacterOutfits = CharacterOutfits.OrderBy(o => o.Name).ToList();

                    OutfitsListMenu.Clear();
                    foreach (Outfit outfit in CharacterOutfits) OutfitsListMenu.AddItem(new UIMenuItem(outfit.Name));
                    OutfitsListMenu.RefreshIndex();

                    File.WriteAllText(path, newOutfit.Serialize());
                    UI.Notify($"Outfit \"{outfitName}\" saved for {currentCharacter}.");
                }
                catch (Exception e)
                {
                    UI.Notify($"~r~Outfit saving failed: {e.Message}");
                }
            };

            // Handle the transition from main to the list menu
            OutfitsMainMenu.OnMenuChange += (oldMenu, newMenu, forward) =>
            {
                if (forward && newMenu == OutfitsListMenu && CharacterOutfits.Count > 0)
                {
                    int outfitItemIdx = newMenu.CurrentSelection;
                    if (outfitItemIdx >= CharacterOutfits.Count)
                    {
                        outfitItemIdx = 0;
                        newMenu.RefreshIndex();
                    }

                    Outfit outfit = CharacterOutfits.FirstOrDefault(o => o.Name == newMenu.MenuItems[outfitItemIdx].Text);
                    if (outfit == null)
                    {
                        UI.Notify("~r~Invalid outfit selected.");
                        return;
                    }

                    outfit.Apply();
                }
            };

            // Handle selection change on the list menu
            OutfitsListMenu.OnIndexChange += (menu, index) =>
            {
                Outfit outfit = CharacterOutfits.FirstOrDefault(o => o.Name == menu.MenuItems[index].Text);
                if (outfit == null)
                {
                    UI.Notify("~r~Invalid outfit selected.");
                    return;
                }

                outfit.Apply();
            };

            // Handle outfit selecting
            OutfitsListMenu.OnItemSelect += (menu, item, index) =>
            {
                Outfit outfit = CharacterOutfits.FirstOrDefault(o => o.Name == item.Text);
                if (outfit == null)
                {
                    UI.Notify("~r~Invalid outfit selected.");
                    return;
                }

                outfit.Apply();
                TempOutfit = outfit;

                UI.Notify($"Now wearing: {outfit.Name}.");
            };

            // Handle the list menu being closed
            OutfitsListMenu.OnMenuClose += (menu) =>
            {
                TempOutfit.Apply();
            };

            OutfitsMenuPool.RefreshIndex();

            // Set up events
            Tick += ScriptTick;
            KeyUp += ScriptKeyUp;
            Aborted += ScriptAborted;
        }
        #endregion

        #region Event: Tick
        public void ScriptTick(object sender, EventArgs e)
        {
            OutfitsMenuPool?.ProcessMenus();
        }
        #endregion

        #region Event: KeyUp
        public void ScriptKeyUp(object sender, KeyEventArgs e)
        {
            // Open the menu
            if (e.KeyCode == MenuKey)
            {
                if (Game.Player.Character.IsInVehicle())
                {
                    UI.Notify("~r~You need to be on foot to access the outfit menu.");
                    return;
                }

                // Open the menu only if it isn't already open
                if (!OutfitsMenuPool.IsAnyMenuOpen())
                {
                    Character currentChar = Methods.GetCurrentCharacter();
                    if (currentChar == Character.Unknown)
                    {
                        UI.Notify("~r~You're not playing as Michael, Franklin or Trevor.");
                        return;
                    }

                    // Detect character change, get outfits of the new character if the character changed
                    if (LastCharacter != currentChar)
                    {
                        CharacterOutfits = AllOutfits.Where(o => o.Character == currentChar).OrderBy(o => o.Name).ToList();

                        OutfitsListMenu.Clear();
                        foreach (Outfit outfit in CharacterOutfits) OutfitsListMenu.AddItem(new UIMenuItem(outfit.Name));
                        OutfitsListMenu.RefreshIndex();

                        int charIdx = (int)currentChar;
                        Sprite sprite = new Sprite(Constants.MenuBanners[charIdx], Constants.MenuBanners[charIdx], Point.Empty, Size.Empty);
                        OutfitsMainMenu.SetBannerType(sprite);
                        OutfitsListMenu.SetBannerType(sprite);

                        LastCharacter = currentChar;
                    }

                    // Store current outfit
                    TempOutfit = Methods.GetCurrentOutfit();
                    TempOutfit.Character = currentChar;

                    // Make the menu visible
                    OutfitsMainMenu.RefreshIndex();
                    OutfitsMainMenu.Visible = true;
                }
            }

            // Delete stored outfit
            if (e.KeyCode == DeleteKey)
            {
                if (OutfitsListMenu.Visible)
                {
                    int outfitIndex = OutfitsListMenu.CurrentSelection;
                    string outfitName = OutfitsListMenu.MenuItems[outfitIndex].Text;
                    Outfit outfitToRemove = CharacterOutfits.FirstOrDefault(o => o.Name == outfitName);

                    if (outfitToRemove == null)
                    {
                        UI.Notify("~r~Outfit removal failed: invalid outfit.");
                        return;
                    }

                    try
                    {
                        OutfitsListMenu.RemoveItemAt(outfitIndex);
                        CharacterOutfits.Remove(outfitToRemove);
                        AllOutfits.Remove(outfitToRemove);
                        OutfitsListMenu.RefreshIndex();

                        File.Delete(outfitToRemove.FilePath);
                        TempOutfit.Apply();
                    }
                    catch (Exception ex)
                    {
                        UI.Notify($"~r~Outfit removal failed: {ex.Message}");
                    }
                }
            }
        }
        #endregion

        #region Event: Aborted
        public void ScriptAborted(object sender, EventArgs e)
        {
            AllOutfits?.Clear();
            CharacterOutfits?.Clear();

            AllOutfits = null;
            CharacterOutfits = null;
            OutfitsMenuPool = null;
            OutfitsMainMenu = null;
            OutfitsListMenu = null;
            TempOutfit = null;
        }
        #endregion
    }
}
