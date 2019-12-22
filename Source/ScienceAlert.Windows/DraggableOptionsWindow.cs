using System.Collections.Generic;
using System.Linq;
using ReeperCommon;
using ScienceAlert.Experiments;
using ScienceAlert.ProfileData;
//ing ScienceAlert.Toolbar;
using UnityEngine;
using KSP.Localization;

namespace ScienceAlert.Windows
{
    public class DraggableOptionsWindow : DraggableWindow
    {
        internal enum OpenPane
        {
            None,
            AdditionalOptions,
            LoadProfiles
        }

        private Vector2 scrollPos; // = default(Vector2);
        private Vector2 additionalScrollPos; // = default(Vector2);
        private Vector2 profileScrollPos; // = Vector2.zero;
        private readonly Dictionary<string, int> experimentIds = new Dictionary<string, int>();
        private readonly List<GUIContent> filterList = new List<GUIContent>();
        private string thresholdValue = "0";
        private OpenPane submenu;
        public ScienceAlert scienceAlert;
        public ExperimentManager manager;
        private Texture2D collapseButton; // = new Texture2D(24, 24);
        private Texture2D expandButton; // = new Texture2D(24, 24);
        private Texture2D openButton; // = new Texture2D(24, 24);
        private Texture2D saveButton; // = new Texture2D(24, 24);
        private Texture2D deleteButton; // = new Texture2D(24, 24);
        private Texture2D renameButton; // = new Texture2D(24, 24);
        private Texture2D blackPixel; // = new Texture2D(1, 1);
        private GUISkin whiteLabel;
        private System.Globalization.NumberFormatInfo formatter;
        private GUIStyle miniLabelLeft;
        private GUIStyle miniLabelRight;
        private GUIStyle miniLabelCenter;
        private AudioPlayer audio;
        internal string editText = string.Empty;
        internal string lockName = string.Empty;
        internal Profile editProfile;
        internal PopupDialog popup;
        internal string badChars = "()[]?'\":#$%^&*~;\n\t\r!@,.{}/<>";

        protected override Rect Setup()
        { 
            scrollPos = default(Vector2);
            additionalScrollPos = default(Vector2);
            profileScrollPos = Vector2.zero;

            collapseButton = new Texture2D(24, 24);
            expandButton = new Texture2D(24, 24);
            openButton = new Texture2D(24, 24);
            saveButton = new Texture2D(24, 24);
            deleteButton = new Texture2D(24, 24);
            renameButton = new Texture2D(24, 24);
            blackPixel = new Texture2D(1, 1);
            
            formatter = (System.Globalization.NumberFormatInfo)System.Globalization.NumberFormatInfo.CurrentInfo.Clone();
            formatter.CurrencySymbol = string.Empty;
            formatter.CurrencyDecimalDigits = 2;
            formatter.NumberDecimalDigits = 2;
            formatter.PercentDecimalDigits = 2;
            audio = AudioPlayer.Audio;
            if (audio == null)
                Log.Debug("[ScienceAlert]:DraggableOptionsWindow: Failed to find AudioPlayer instance");

            filterList.Add(new GUIContent(Localizer.Format("#ScienceAlert_button1")));//"Unresearched"
            filterList.Add(new GUIContent(Localizer.Format("#ScienceAlert_button2")));//"Not maxed"
            filterList.Add(new GUIContent(Localizer.Format("#ScienceAlert_button3")));//"< 50% collected"
            filterList.Add(new GUIContent(Localizer.Format("#ScienceAlert_button4")));//"< 90% collected"

            openButton = ResourceUtil.LoadImage("btnOpen.png");
            deleteButton = ResourceUtil.LoadImage("btnDelete.png");
            renameButton = ResourceUtil.LoadImage("btnRename.png");
            saveButton = ResourceUtil.LoadImage("btnSave.png");
            expandButton = ResourceUtil.LoadImage("btnExpand.png");
            collapseButton = Instantiate(expandButton);
            ResourceUtil.FlipTexture(collapseButton, true, true);
            collapseButton.Compress(false);
            expandButton.Compress(false);

            blackPixel.SetPixel(0, 0, Color.black);
            blackPixel.Apply();
            blackPixel.filterMode = FilterMode.Bilinear;
            whiteLabel = Instantiate(Settings.Skin);
            whiteLabel.label.onNormal.textColor = Color.white;
            whiteLabel.toggle.onNormal.textColor = Color.white;
            whiteLabel.label.onActive.textColor = Color.white;
            submenu = OpenPane.None;
            Title = Localizer.Format("#ScienceAlert_Optitle");//"ScienceAlert Options"
            miniLabelLeft = new GUIStyle(Skin.label) { fontSize = 10 };
            miniLabelLeft.normal.textColor = miniLabelLeft.onNormal.textColor = Color.white;
            miniLabelRight = new GUIStyle(miniLabelLeft) { alignment = TextAnchor.MiddleRight };
            miniLabelCenter = new GUIStyle(miniLabelLeft) { alignment = TextAnchor.MiddleCenter };
            Settings.Instance.OnSave += OnAboutToSave;
            OnVisibilityChange += OnVisibilityChanged;
            GameEvents.onVesselChange.Add(OnVesselChanged);
            LoadFrom(Settings.Instance.additional.GetNode("OptionsWindow") ?? new ConfigNode());
            return new Rect(windowRect.x, windowRect.y, 324f, Screen.height / 5 * 3);
        }

        protected new void OnDestroy()
        {
            base.OnDestroy();
            OnVisibilityChange -= OnVisibilityChanged;
        }

        private void OnVisibilityChanged(bool tf)
        {
            if (tf)
            {
                OnProfileChanged();
                return;
            }
            if (manager == null) return;
            manager.RebuildObserverList();
        }

        public void OnProfileChanged()
        {
            if (ScienceAlertProfileManager.ActiveProfile == null) return;
            thresholdValue = ScienceAlertProfileManager.ActiveProfile.ScienceThreshold.ToString("F2", formatter);
            List<string> experimentIDs = ResearchAndDevelopment.GetExperimentIDs();
            IOrderedEnumerable<string> orderedEnumerable = from expid in experimentIDs
                                                           orderby ResearchAndDevelopment.GetExperiment(expid).experimentTitle
                                                           select expid;
            experimentIds.Clear();
            using (IEnumerator<string> enumerator = orderedEnumerable.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    string current = enumerator.Current;
                    experimentIds.Add(current, (int)System.Convert.ChangeType(ScienceAlertProfileManager.ActiveProfile[current].Filter,
                        ScienceAlertProfileManager.ActiveProfile[current].Filter.GetTypeCode()));
                }
            }
        }

        private void OnVesselChanged(Vessel vessel)
        {
            OnVisibilityChanged(Visible);
        }

        protected override void OnCloseClick()
        {
            Visible = false;
        }

        private void OnAboutToSave()
        {
            SaveInto(Settings.Instance.additional.GetNode("OptionsWindow") ?? Settings.Instance.additional.AddNode("OptionsWindow"));
        }

        protected override void DrawUI()
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.Height(Screen.height / 5 * 3));
            GUILayout.Label(new GUIContent(Localizer.Format("#ScienceAlert_label1")), GUILayout.ExpandWidth(true));//"Global Warp Settings"
            Settings.Instance.GlobalWarp = (Settings.WarpSetting)GUILayout.SelectionGrid((int)Settings.Instance.GlobalWarp, new[]
            {
                new GUIContent(Localizer.Format("#ScienceAlert_button5")),//"By Experiment"
                new GUIContent(Localizer.Format("#ScienceAlert_button6")),//"Globally on"
                new GUIContent(Localizer.Format("#ScienceAlert_button7"))//"Globally off"
            }, 3, GUILayout.ExpandWidth(false));
            GUILayout.Label(new GUIContent(Localizer.Format("#ScienceAlert_label2")), GUILayout.ExpandWidth(true));//"Global Alert Sound"
            Settings.Instance.SoundNotification = (Settings.SoundNotifySetting)GUILayout.SelectionGrid((int)Settings.Instance.SoundNotification, new[]
            {
                new GUIContent(Localizer.Format("#ScienceAlert_button5")),//"By Experiment"
                new GUIContent(Localizer.Format("#ScienceAlert_button8")),//"Always"
                new GUIContent(Localizer.Format("#ScienceAlert_button9"))//"Never"
            }, 3, GUILayout.ExpandWidth(false));
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(Localizer.Format("#ScienceAlert_label3")));//"Additional Options"
            GUILayout.FlexibleSpace();
            if (AudibleButton(new GUIContent(submenu == OpenPane.AdditionalOptions ? collapseButton : expandButton)))
            {
                submenu = submenu == OpenPane.AdditionalOptions ? OpenPane.None : OpenPane.AdditionalOptions;
            }
            GUILayout.EndHorizontal();
            switch (submenu)
            {
                case OpenPane.None:
                    DrawProfileSettings();
                    break;
                case OpenPane.AdditionalOptions:
                    DrawAdditionalOptions();
                    break;
                case OpenPane.LoadProfiles:
                    DrawProfileList();
                    break;
            }
            GUILayout.EndVertical();
        }

        private void DrawAdditionalOptions()
        {
            additionalScrollPos = GUILayout.BeginScrollView(additionalScrollPos, GUILayout.ExpandHeight(true));
            GUILayout.Space(4f);
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.Box(Localizer.Format("#ScienceAlert_label4"), GUILayout.ExpandWidth(true));//"User Interface Settings"
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.Label(Localizer.Format("#ScienceAlert_label5"), GUILayout.ExpandWidth(true));//"Globally Enable Animation"
            Settings.Instance.FlaskAnimationEnabled = AudibleToggle(Settings.Instance.FlaskAnimationEnabled, string.Empty, null, new[]
            {
                GUILayout.ExpandWidth(false)
            });
            if (!Settings.Instance.FlaskAnimationEnabled && ScienceAlert.Instance.IsAnimating)
            {
                ScienceAlert.Instance.SetLit();
            }
            GUILayout.EndHorizontal();
            Settings.Instance.ShowReportValue = AudibleToggle(Settings.Instance.ShowReportValue, Localizer.Format("#ScienceAlert_toggle1"));//"Display Report Value"
            Settings.Instance.DisplayCurrentBiome = AudibleToggle(Settings.Instance.DisplayCurrentBiome, Localizer.Format("#ScienceAlert_toggle2"));//"Display Biome in Experiment List"
            Settings.Instance.EvaReportOnTop = AudibleToggle(Settings.Instance.EvaReportOnTop, Localizer.Format("#ScienceAlert_toggle3"));//"List EVA Report first"
            GUILayout.Label(Localizer.Format("#ScienceAlert_label6"));//"Window Opacity"
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#ScienceAlert_label7"), miniLabelLeft);//"Less"
            GUILayout.FlexibleSpace();
            GUILayout.Label(Localizer.Format("#ScienceAlert_label8"), miniLabelRight);//"More"
            GUILayout.EndHorizontal();
            Settings.Instance.WindowOpacity = (int)GUILayout.HorizontalSlider(Settings.Instance.WindowOpacity, 0f, 255f, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(16f));
            GUILayout.Space(8f);
            GUILayout.Box(Localizer.Format("#ScienceAlert_label9"), GUILayout.ExpandWidth(true));//"Third-party Integration Options"
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            Settings.ScanInterface scanInterfaceType = Settings.Instance.ScanInterfaceType;
            Color color = GUI.color;
            if (!SCANsatInterface.IsAvailable())
            {
                GUI.color = Color.red;
            }
            bool flag = AudibleToggle(Settings.Instance.ScanInterfaceType == Settings.ScanInterface.ScanSat, Localizer.Format("#ScienceAlert_toggle4"), null, new[]//"Enable SCANsat integration"
            {
                GUILayout.ExpandWidth(true)
            });
            GUI.color = color;
            if (flag && scanInterfaceType != Settings.ScanInterface.ScanSat && !SCANsatInterface.IsAvailable())
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Localizer.Format("#ScienceAlert_Msg1title")
                    , Localizer.Format("#ScienceAlert_Msg1"), "", Localizer.Format("#ScienceAlert_Msg1_button1"),//"SCANsat Not Found""SCANsat was not found. You must install SCANsat to use this feature.""Okay"
                    false, HighLogic.UISkin);
                flag = false;
            }
            Settings.Instance.ScanInterfaceType = flag ? Settings.ScanInterface.ScanSat : Settings.ScanInterface.None;
            scienceAlert.ScanInterfaceType = Settings.Instance.ScanInterfaceType;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            //Settings.ToolbarInterface toolbarInterfaceType = Settings.Instance.ToolbarInterfaceType;
            Color color2 = GUI.color;

            //bool flag2 = AudibleToggle(Settings.Instance.ToolbarInterfaceType == Settings.ToolbarInterface.BlizzyToolbar, "Use Blizzy toolbar");
            GUI.color = color2;
            //if (flag2 && toolbarInterfaceType != Settings.ToolbarInterface.BlizzyToolbar && !ToolbarManager.ToolbarAvailable)
            //{
            //             PopupDialog.SpawnPopupDialog("Blizzy Toolbar Not Found",
            //                 "Blizzy's toolbar was not found. You must install Blizzy's toolbar to use this feature.",
            //                 "Okay", false, Settings.Skin); //???
            //             flag2 = false;
            //}
            //Settings.Instance.ToolbarInterfaceType = (flag2 ? Settings.ToolbarInterface.BlizzyToolbar : Settings.ToolbarInterface.ApplicationLauncher);

            GUILayout.EndHorizontal();
            GUILayout.Box(Localizer.Format("#ScienceAlert_label10"), GUILayout.ExpandWidth(true));//"Crewed Vessel Settings"
            bool checkSurfaceSampleNotEva = Settings.Instance.CheckSurfaceSampleNotEva;
            Settings.Instance.CheckSurfaceSampleNotEva = AudibleToggle(checkSurfaceSampleNotEva, Localizer.Format("#ScienceAlert_toggle5"));//"Track surface sample in vessel"
            if (checkSurfaceSampleNotEva != Settings.Instance.CheckSurfaceSampleNotEva)
            {
                manager.RebuildObserverList();
            }
            GUILayout.EndVertical();
            GUI.skin = Settings.Skin;
            GUILayout.EndScrollView();
        }

        private void DrawProfileSettings()
        {
            if (ScienceAlertProfileManager.HasActiveProfile)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Box(Localizer.Format("#ScienceAlert_label11", ScienceAlertProfileManager.ActiveProfile.DisplayName), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));//$"Profile: <<1>>{}"
                if (AudibleButton(new GUIContent(renameButton), GUILayout.MaxWidth(24f)))
                {
                    SpawnRenamePopup(ScienceAlertProfileManager.ActiveProfile);
                }
                GUI.enabled = ScienceAlertProfileManager.ActiveProfile.modified;
                if (AudibleButton(new GUIContent(saveButton), GUILayout.MaxWidth(24f)))
                {
                    SpawnSavePopup();
                }
                GUI.enabled = true;
                if (AudibleButton(new GUIContent(openButton), GUILayout.MaxWidth(24f)))
                {
                    submenu = OpenPane.LoadProfiles;
                }
                GUILayout.EndHorizontal();
                scrollPos = GUILayout.BeginScrollView(scrollPos, Settings.Skin.scrollView);
                GUI.skin = Settings.Skin;
                GUILayout.Space(4f);
                GUI.SetNextControlName("ThresholdHeader");
                GUILayout.Box(Localizer.Format("#ScienceAlert_label12"));//"Alert Threshold"
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.MinHeight(14f));
                if (ScienceAlertProfileManager.ActiveProfile.ScienceThreshold > 0f)
                {
                    GUILayout.Label(Localizer.Format("#ScienceAlert_label13", ScienceAlertProfileManager.ActiveProfile.ScienceThreshold.ToString("F2", formatter)));//$"Alert Threshold: <<1>>"
                }
                else
                {
                    Color color = GUI.color;
                    GUI.color = XKCDColors.Salmon;
                    GUILayout.Label(Localizer.Format("#ScienceAlert_label14"));//"(disabled)"
                    GUI.color = color;
                }
                GUILayout.FlexibleSpace();
                if (string.IsNullOrEmpty(thresholdValue))
                {
                    thresholdValue = ScienceAlertProfileManager.ActiveProfile.scienceThreshold.ToString("F2", formatter);
                }
                GUI.SetNextControlName("ThresholdText");
                string s = GUILayout.TextField(thresholdValue, GUILayout.MinWidth(60f));
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    GUI.FocusControl("ThresholdHeader");
                }
                if (GUI.GetNameOfFocusedControl() == "ThresholdText")
                {
                    try
                    {
                        float scienceThreshold = float.Parse(s, formatter);
                        ScienceAlertProfileManager.ActiveProfile.ScienceThreshold = scienceThreshold;
                        thresholdValue = s;
                    }
                    catch (System.Exception)
                    {
                        // ignored
                    }
                    if (!InputLockManager.IsLocked(ControlTypes.ACTIONS_ALL))
                    {
                        InputLockManager.SetControlLock(ControlTypes.ACTIONS_ALL, "ScienceAlertThreshold");
                    }
                }
                else if (InputLockManager.GetControlLock("ScienceAlertThreshold") != ControlTypes.None)
                {
                    InputLockManager.RemoveControlLock("ScienceAlertThreshold");
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(3f);
                var num = GUILayout.HorizontalSlider(ScienceAlertProfileManager.ActiveProfile.ScienceThreshold, 0f, 100f, GUILayout.ExpandWidth(true), GUILayout.Height(14f));
                if (num != ScienceAlertProfileManager.ActiveProfile.scienceThreshold)
                {
                    ScienceAlertProfileManager.ActiveProfile.ScienceThreshold = num;
                    thresholdValue = num.ToString("F2", formatter);
                }
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.MaxHeight(10f));
                GUILayout.Label("0", miniLabelLeft);
                GUILayout.FlexibleSpace();
                GUILayout.Label(Localizer.Format("#ScienceAlert_label15"), miniLabelCenter);//"Science Amount"
                GUILayout.FlexibleSpace();
                GUILayout.Label("100", miniLabelRight);
                GUILayout.EndHorizontal();
                GUILayout.Space(10f);
                List<string> list = new List<string>(experimentIds.Keys);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    string current = list[i];

                    GUILayout.Space(4f);
                    ExperimentSettings experimentSettings = ScienceAlertProfileManager.ActiveProfile[current];
                    string experimentTitle = ResearchAndDevelopment.GetExperiment(current).experimentTitle;
                    GUILayout.Box(experimentTitle, GUILayout.ExpandWidth(true));
                    experimentSettings.Enabled = AudibleToggle(experimentSettings.Enabled, Localizer.Format("#ScienceAlert_toggle6"));//"Enabled"
                    experimentSettings.AnimationOnDiscovery = AudibleToggle(experimentSettings.AnimationOnDiscovery, Localizer.Format("#ScienceAlert_toggle7"));//"Animation on discovery"
                    experimentSettings.SoundOnDiscovery = AudibleToggle(experimentSettings.SoundOnDiscovery, Localizer.Format("#ScienceAlert_toggle8"));//"Sound on discovery"
                    experimentSettings.StopWarpOnDiscovery = AudibleToggle(experimentSettings.StopWarpOnDiscovery, Localizer.Format("#ScienceAlert_toggle9"));//"Stop warp on discovery"
                    GUILayout.Label(new GUIContent(Localizer.Format("#ScienceAlert_toggle10")), GUILayout.ExpandWidth(true), GUILayout.MinHeight(24f));//"Filter Method"
                    int num2 = experimentIds[current];
                    experimentIds[current] = AudibleSelectionGrid(num2, ref experimentSettings);
                }
                GUILayout.EndScrollView();
                return;
            }
            GUI.color = Color.red;
            GUILayout.Label(Localizer.Format("#ScienceAlert_label16"));//"No profile active"
        }

        private void DrawProfileList()
        {
            profileScrollPos = GUILayout.BeginScrollView(profileScrollPos, Settings.Skin.scrollView);
            bool profilesExist = false;
            if (ScienceAlertProfileManager.Count > 0)
            {
                GUILayout.Label(Localizer.Format("#ScienceAlert_label17"));//"Select a profile to load"
                GUILayout.Box(blackPixel, GUILayout.ExpandWidth(true), GUILayout.MinHeight(1f), GUILayout.MaxHeight(3f));
                GUILayout.Space(4f);
                Dictionary<string, Profile> profiles = ScienceAlertProfileManager.Profiles;
                DrawProfileList_ListItem(ScienceAlertProfileManager.DefaultProfile);

                foreach (var current in profiles.Values)
                {
                    if (current != ScienceAlertProfileManager.DefaultProfile)
                    {
                        DrawProfileList_ListItem(current);
                        profilesExist = true;
                    }
                }

                // Uuugh   ungly GOTO, must get rid of it
#if false
                using (Dictionary<string, Profile>.ValueCollection.Enumerator enumerator = profiles.Values.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Profile current = enumerator.Current;
                        if (current != ScienceAlertProfileManager.DefaultProfile)
                        {
                            DrawProfileList_ListItem(current);
                        }
                    }
                    goto IL_F1;
                }
#endif
            }
            if (!profilesExist)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Box(Localizer.Format("#ScienceAlert_label18"), GUILayout.MinHeight(64f));//"No profiles saved"
                GUILayout.FlexibleSpace();
            }
            //IL_F1:
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (AudibleButton(new GUIContent(Localizer.Format("#ScienceAlert_button10"), "Cancel load operation")))//"Cancel"
            {
                submenu = OpenPane.None;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private void DrawProfileList_ListItem(Profile profile)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Box(profile.name, GUILayout.ExpandWidth(true));
            GUI.enabled = profile != ScienceAlertProfileManager.DefaultProfile;
            if (AudibleButton(new GUIContent(renameButton), GUILayout.MaxWidth(24f), GUILayout.MinWidth(24f)))
            {
                SpawnRenamePopup(profile);
            }
            GUI.enabled = true;
            if (AudibleButton(new GUIContent(openButton), GUILayout.MaxWidth(24f), GUILayout.MinWidth(24f)))
            {
                SpawnOpenPopup(profile);
            }
            GUI.enabled = profile != ScienceAlertProfileManager.DefaultProfile;
            if (AudibleButton(new GUIContent(deleteButton), GUILayout.MaxWidth(24f), GUILayout.MinWidth(24f)))
            {
                SpawnDeletePopup(profile);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private bool AudibleToggle(bool value, string content, GUIStyle style = null, GUILayoutOption[] options = null)
        {
            return AudibleToggle(value, new GUIContent(content), style, options);
        }

        private bool AudibleToggle(bool value, GUIContent content, GUIStyle style = null, GUILayoutOption[] options = null)
        {
            bool flag = GUILayout.Toggle(value, content, style == null ? Settings.Skin.toggle : style, options);
            if (flag != value)
                audio.PlayUI("click1");
            return flag;
        }

        private int AudibleSelectionGrid(int currentValue, ref ExperimentSettings settings)
        {
            int num = GUILayout.SelectionGrid(currentValue, filterList.ToArray(), 2, GUILayout.ExpandWidth(true));
            if (num == currentValue) return num;
            audio.PlayUI("click1");
            settings.Filter = (ExperimentSettings.FilterMethod)num;
            return num;
        }

        private bool AudibleButton(GUIContent content, params GUILayoutOption[] options)
        {
            bool flag = GUILayout.Button(content, options);
            if (!flag) return false;
            audio.PlayUI("click1");
            return true;
        }

        private void SpawnSavePopup()
        {
            editText = ScienceAlertProfileManager.ActiveProfile.name;
            LockControls("ScienceAlertSavePopup");

            DialogGUIBase[] dialogOptions = new DialogGUIBase[2];
            dialogOptions[0] = new DialogGUIButton(Localizer.Format("#ScienceAlert_button11"), SaveCurrentProfile);//"SAVE"
            dialogOptions[1] = new DialogGUIButton(Localizer.Format("#ScienceAlert_button12"), DismissPopup);//"CANCEL"

            popup = PopupDialog.SpawnPopupDialog(new MultiOptionDialog("", "", Localizer.Format("#ScienceAlert_label19", editText),//$"SAVE '{}'?"
                HighLogic.UISkin, dialogOptions),
                false, HighLogic.UISkin);
        }

        private void SaveCurrentProfile()
        {
            if (popup != null)
                popup.Dismiss();
            else
                editText = ScienceAlertProfileManager.ActiveProfile.name;

            // Confirm overwrite an existing non-active profile
            if (ScienceAlertProfileManager.HaveStoredProfile(editText) && ScienceAlertProfileManager.ActiveProfile.name != editText)
            {
                popup = PopupDialog.SpawnPopupDialog(new MultiOptionDialog("", "",
                        Localizer.Format("#ScienceAlert_label20", editText), HighLogic.UISkin,//$"Profile '{}' already exists!"
                    new DialogGUIButton(Localizer.Format("#ScienceAlert_button13"), SaveCurrentProfileOverwrite),//"Overwrite"
                    new DialogGUIButton(Localizer.Format("#ScienceAlert_button10"), DismissPopup)),//"Cancel"
                    false, HighLogic.UISkin);
            }
            else
                SaveCurrentProfileOverwrite(); // save to go ahead and save since no existing profile with this key exists
        }

        private void SaveCurrentProfileOverwrite()
        {
            ScienceAlertProfileManager.StoreActiveProfile(editText);
            Settings.Instance.Save();
            DismissPopup();
        }

        private void SpawnRenamePopup(Profile target)
        {
            editProfile = target;
            editText = target.name;
            LockControls("ScienceAlertRenamePopup");

            DialogGUIBase[] dialogOptions = new DialogGUIBase[3];
            dialogOptions[0] = new DialogGUITextInput(editText, false, 22, s => { editText = s; return s; }, 30f);
            dialogOptions[1] = new DialogGUIButton(Localizer.Format("#ScienceAlert_button14"), RenameTargetProfile);//"RENAME"
            dialogOptions[2] = new DialogGUIButton(Localizer.Format("#ScienceAlert_button10"), DismissPopup);//"CANCEL"

            popup = PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog("", "", Localizer.Format("#ScienceAlert_Msg2title", target.name),//$"Rename '{}' to:"
                HighLogic.UISkin, dialogOptions),
                false, HighLogic.UISkin);
        }

        private void RenameTargetProfile()
        {
            if (editProfile.modified || !ScienceAlertProfileManager.HaveStoredProfile(editProfile.name))
            {
                RenameTargetProfileOverwrite();
            }
            else
            {
                if (ScienceAlertProfileManager.HaveStoredProfile(editText))
                {
                    popup.Dismiss();
                    popup = PopupDialog.SpawnPopupDialog(
                        new MultiOptionDialog(string.Empty, Localizer.Format("#ScienceAlert_Msg2", editText), Localizer.Format("#ScienceAlert_Msg2title2"), HighLogic.UISkin, //$"'{}' already exists. Overwrite?""RenameTargetProfile"
                        new DialogGUIButton(Localizer.Format("#ScienceAlert_Msg2_button1"), RenameTargetProfileOverwrite),//"Yes"
                        new DialogGUIButton(Localizer.Format("#ScienceAlert_Msg2_button2"), DismissPopup)),//"No"
                        false, HighLogic.UISkin);
                    return;
                }
                RenameTargetProfileOverwrite();
            }
            SpawnSavePopup();
            DismissPopup();
        }

        private void RenameTargetProfileOverwrite()
        {
            if (!editProfile.modified && ScienceAlertProfileManager.HaveStoredProfile(editProfile.name))
            {
                ScienceAlertProfileManager.RenameProfile(editProfile.name, editText);
                if (!ScienceAlertProfileManager.ActiveProfile.modified)
                {
                    ScienceAlertProfileManager.ActiveProfile.name = editText;
                }
            }
            else
            {
                editProfile.name = editText;
                editProfile.modified = true;
            }
            DismissPopup();
        }

        private void SpawnDeletePopup(Profile target)
        {
            editProfile = target;
            LockControls("ScienceAlertDeletePopup");
            popup = PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog("", "", Localizer.Format("#ScienceAlert_Msg3", target.name), HighLogic.UISkin, //$"Are you sure you want to\ndelete '{}'?"
                new DialogGUIButton(Localizer.Format("#ScienceAlert_button15"), DeleteTargetProfile), //"Confirm"
                new DialogGUIButton(Localizer.Format("#ScienceAlert_button10"), DismissPopup)),//"Cancel"
                false, HighLogic.UISkin);
        }

        private void DeleteTargetProfile()
        {
            DismissPopup();
            ScienceAlertProfileManager.DeleteProfile(editProfile.name);
        }

        private void SpawnOpenPopup(Profile target)
        {
            editProfile = target;
            LockControls("ScienceAlertOpenPopup");
            popup = PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(string.Empty, Localizer.Format("#ScienceAlert_Msg3_2", editProfile.name),//$"Load '{}'?\nUnsaved settings will be lost."
                Localizer.Format("#ScienceAlert_Msg3title"), HighLogic.UISkin,//"Science Alert Open Popup"
                new DialogGUIButton(Localizer.Format("#ScienceAlert_button15"), LoadTargetProfile),//"Confirm"
                new DialogGUIButton(Localizer.Format("#ScienceAlert_button10"), DismissPopup)),//"Cancel"
                false, HighLogic.UISkin);
        }

        private void LoadTargetProfile()
        {
            DismissPopup();
            if (!ScienceAlertProfileManager.AssignAsActiveProfile(editProfile.Clone())) return;
            submenu = OpenPane.None;
            OnVisibilityChanged(Visible);
        }

        private void LockControls(string lockName)
        {
            this.lockName = lockName;
            InputLockManager.SetControlLock(ControlTypes.ACTIONS_ALL, lockName);
        }

        private void DismissPopup()
        {
            if (popup) popup.Dismiss();
            InputLockManager.RemoveControlLock(lockName);
            lockName = string.Empty;
        }
    }
}
