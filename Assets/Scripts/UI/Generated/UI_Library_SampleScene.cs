// ========================================
// AUTO-GENERATED CODE - DO NOT MODIFY
// ========================================
// Scene: SampleScene
// Generated: 2025-10-05 20:58:50
// UI Framework v2.0 - Dot Notation
// ========================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace CodeSculptLabs.UIFramework.SampleScene
{
    /// <summary>
    /// Auto-generated UI library for SampleScene.
    /// Provides type-safe dot notation access to all UI elements.
    /// Usage: UI.Initialize(uiManager); then access via UI.PanelName.ElementName
    /// </summary>
    public static class UI
    {
        private static UIManager _manager;

        /// <summary>
        /// Initialize the UI library with a UIManager instance.
        /// Call this once at startup (e.g., in Awake or Start).
        /// </summary>
        public static void Initialize(UIManager manager)
        {
            _manager = manager;
            if (_manager == null)
            {
                Debug.LogError("[UI Library] UIManager is null! UI elements will not be accessible.");
            }
        }


        #region Login

        /// <summary>
        /// Login panel and its UI elements.
        /// </summary>
        public static class Login
        {

            /// <summary>
            /// Shows this panel.
            /// </summary>
            /// <param name="hideOthers">Hide all other panels</param>
            public static void Show(bool hideOthers = true)
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                _manager.SetPanelActive("UI_Canvas/Login_Panel", true, deactivateOthers: hideOthers);
            }

            /// <summary>
            /// Hides this panel.
            /// </summary>
            public static void Hide()
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                _manager.SetPanelActive("UI_Canvas/Login_Panel", false);
            }

            /// <summary>
            /// Toggles this panel's visibility.
            /// </summary>
            public static void Toggle()
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                var panel = _manager.GetPanel("UI_Canvas/Login_Panel");
                if (panel != null) panel.SetActive(!panel.activeSelf);
            }

            /// <summary>
            /// Gets whether this panel is currently visible.
            /// </summary>
            public static bool IsVisible
            {
                get
                {
                    if (_manager == null) return false;
                    var panel = _manager.GetPanel("UI_Canvas/Login_Panel");
                    return panel != null && panel.activeSelf;
                }
            }

            #region InputField

            /// <summary>
            /// Username_InputField (InputField)
            /// </summary>
            public static InputField Username_InputField
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_InputField");
                        return null;
                    }
                    return _manager.GetUIComponent<InputField>("UI_Canvas/Login_Panel/Login_Panel_Username_InputField");
                }
            }
            #endregion

            #region Text

            /// <summary>
            /// Username_Inputfield_Placeholder_Text (Text)
            /// </summary>
            public static Text Username_Inputfield_Placeholder_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_Inputfield_Placeholder_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/Login_Panel/Login_Panel_Username_InputField/Login_Panel_Username_Inputfield_Placeholder_Text");
                }
            }

            /// <summary>
            /// Username_InputField_Text (Text)
            /// </summary>
            public static Text Username_InputField_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_InputField_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/Login_Panel/Login_Panel_Username_InputField/Login_Panel_Username_InputField_Text");
                }
            }
            #endregion
        }

        #endregion

        #region Main

        /// <summary>
        /// Main panel and its UI elements.
        /// </summary>
        public static class Main
        {

            #region Button

            /// <summary>
            /// Exit_Button (Button)
            /// </summary>
            public static Button Exit_Button
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Exit_Button");
                        return null;
                    }
                    return _manager.GetUIComponent<Button>("UI_Canvas/Main_Menu_Panel/Main_Menu_Exit_Button");
                }
            }

            /// <summary>
            /// Play_Button (Button)
            /// </summary>
            public static Button Play_Button
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Play_Button");
                        return null;
                    }
                    return _manager.GetUIComponent<Button>("UI_Canvas/Main_Menu_Panel/Main_Menu_Play_Button");
                }
            }
            #endregion

            #region Text

            /// <summary>
            /// Exit_Button_Text (Text)
            /// </summary>
            public static Text Exit_Button_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Exit_Button_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/Main_Menu_Panel/Main_Menu_Exit_Button/Main_Menu_Exit_Button_Text");
                }
            }

            /// <summary>
            /// Play_Button_Text (Text)
            /// </summary>
            public static Text Play_Button_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Play_Button_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/Main_Menu_Panel/Main_Menu_Play_Button/Main_Menu_Play_Button_Text");
                }
            }
            #endregion
        }

        #endregion

        #region MainMenu

        /// <summary>
        /// MainMenu panel and its UI elements.
        /// </summary>
        public static class MainMenu
        {

            /// <summary>
            /// Shows this panel.
            /// </summary>
            /// <param name="hideOthers">Hide all other panels</param>
            public static void Show(bool hideOthers = true)
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                _manager.SetPanelActive("UI_Canvas/Main_Menu_Panel", true, deactivateOthers: hideOthers);
            }

            /// <summary>
            /// Hides this panel.
            /// </summary>
            public static void Hide()
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                _manager.SetPanelActive("UI_Canvas/Main_Menu_Panel", false);
            }

            /// <summary>
            /// Toggles this panel's visibility.
            /// </summary>
            public static void Toggle()
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                var panel = _manager.GetPanel("UI_Canvas/Main_Menu_Panel");
                if (panel != null) panel.SetActive(!panel.activeSelf);
            }

            /// <summary>
            /// Gets whether this panel is currently visible.
            /// </summary>
            public static bool IsVisible
            {
                get
                {
                    if (_manager == null) return false;
                    var panel = _manager.GetPanel("UI_Canvas/Main_Menu_Panel");
                    return panel != null && panel.activeSelf;
                }
            }

            #region InputField

            /// <summary>
            /// Panel_Username_InputField (InputField)
            /// </summary>
            public static InputField Panel_Username_InputField
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Panel_Username_InputField");
                        return null;
                    }
                    return _manager.GetUIComponent<InputField>("UI_Canvas/Main_Menu_Panel/Main_Menu_Panel_Username_InputField");
                }
            }
            #endregion

            #region Text

            /// <summary>
            /// Panel_Username_Inputfield_Placeholder_Text (Text)
            /// </summary>
            public static Text Panel_Username_Inputfield_Placeholder_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Panel_Username_Inputfield_Placeholder_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/Main_Menu_Panel/Main_Menu_Panel_Username_InputField/Main_Menu_Panel_Username_Inputfield_Placeholder_Text");
                }
            }

            /// <summary>
            /// Panel_Username_InputField_Text (Text)
            /// </summary>
            public static Text Panel_Username_InputField_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Panel_Username_InputField_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/Main_Menu_Panel/Main_Menu_Panel_Username_InputField/Main_Menu_Panel_Username_InputField_Text");
                }
            }
            #endregion
        }

        #endregion

        #region Profile

        /// <summary>
        /// Profile panel and its UI elements.
        /// </summary>
        public static class Profile
        {

            /// <summary>
            /// Shows this panel.
            /// </summary>
            /// <param name="hideOthers">Hide all other panels</param>
            public static void Show(bool hideOthers = true)
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                _manager.SetPanelActive("UI_Canvas/Profile_Panel", true, deactivateOthers: hideOthers);
            }

            /// <summary>
            /// Hides this panel.
            /// </summary>
            public static void Hide()
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                _manager.SetPanelActive("UI_Canvas/Profile_Panel", false);
            }

            /// <summary>
            /// Toggles this panel's visibility.
            /// </summary>
            public static void Toggle()
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                var panel = _manager.GetPanel("UI_Canvas/Profile_Panel");
                if (panel != null) panel.SetActive(!panel.activeSelf);
            }

            /// <summary>
            /// Gets whether this panel is currently visible.
            /// </summary>
            public static bool IsVisible
            {
                get
                {
                    if (_manager == null) return false;
                    var panel = _manager.GetPanel("UI_Canvas/Profile_Panel");
                    return panel != null && panel.activeSelf;
                }
            }

            #region InputField

            /// <summary>
            /// Username_InputField (InputField)
            /// </summary>
            public static InputField Username_InputField
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_InputField");
                        return null;
                    }
                    return _manager.GetUIComponent<InputField>("UI_Canvas/Profile_Panel/Profile_Panel_Username_InputField");
                }
            }
            #endregion

            #region Text

            /// <summary>
            /// Username_Inputfield_Placeholder_Text (Text)
            /// </summary>
            public static Text Username_Inputfield_Placeholder_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_Inputfield_Placeholder_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/Profile_Panel/Profile_Panel_Username_InputField/Profile_Panel_Username_Inputfield_Placeholder_Text");
                }
            }

            /// <summary>
            /// Username_InputField_Text (Text)
            /// </summary>
            public static Text Username_InputField_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_InputField_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/Profile_Panel/Profile_Panel_Username_InputField/Profile_Panel_Username_InputField_Text");
                }
            }
            #endregion
        }

        #endregion

        #region SignUp

        /// <summary>
        /// SignUp panel and its UI elements.
        /// </summary>
        public static class SignUp
        {

            /// <summary>
            /// Shows this panel.
            /// </summary>
            /// <param name="hideOthers">Hide all other panels</param>
            public static void Show(bool hideOthers = true)
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                _manager.SetPanelActive("UI_Canvas/SignUp_Panel", true, deactivateOthers: hideOthers);
            }

            /// <summary>
            /// Hides this panel.
            /// </summary>
            public static void Hide()
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                _manager.SetPanelActive("UI_Canvas/SignUp_Panel", false);
            }

            /// <summary>
            /// Toggles this panel's visibility.
            /// </summary>
            public static void Toggle()
            {
                if (_manager == null) { Debug.LogError("[UI] UIManager not initialized!"); return; }
                var panel = _manager.GetPanel("UI_Canvas/SignUp_Panel");
                if (panel != null) panel.SetActive(!panel.activeSelf);
            }

            /// <summary>
            /// Gets whether this panel is currently visible.
            /// </summary>
            public static bool IsVisible
            {
                get
                {
                    if (_manager == null) return false;
                    var panel = _manager.GetPanel("UI_Canvas/SignUp_Panel");
                    return panel != null && panel.activeSelf;
                }
            }

            #region InputField

            /// <summary>
            /// Username_InputField (InputField)
            /// </summary>
            public static InputField Username_InputField
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_InputField");
                        return null;
                    }
                    return _manager.GetUIComponent<InputField>("UI_Canvas/SignUp_Panel/SignUp_Panel_Username_InputField");
                }
            }
            #endregion

            #region Text

            /// <summary>
            /// Username_Inputfield_Placeholder_Text (Text)
            /// </summary>
            public static Text Username_Inputfield_Placeholder_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_Inputfield_Placeholder_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/SignUp_Panel/SignUp_Panel_Username_InputField/SignUp_Panel_Username_Inputfield_Placeholder_Text");
                }
            }

            /// <summary>
            /// Username_InputField_Text (Text)
            /// </summary>
            public static Text Username_InputField_Text
            {
                get
                {
                    if (_manager == null)
                    {
                        Debug.LogError("[UI] UIManager not initialized! Cannot access Username_InputField_Text");
                        return null;
                    }
                    return _manager.GetUIComponent<Text>("UI_Canvas/SignUp_Panel/SignUp_Panel_Username_InputField/SignUp_Panel_Username_InputField_Text");
                }
            }
            #endregion
        }

        #endregion
    }
}
