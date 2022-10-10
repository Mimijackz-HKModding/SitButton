using Modding;
using Modding.Menu;
using Modding.Menu.Config;
using System;
using UnityEngine;
using System.Collections.Generic;
using SceneManagement = UnityEngine.SceneManagement;
using InControl;

namespace SitButton
{
    public class SitButton : Mod, ICustomMenuMod, IGlobalSettings<SitButtonSettings>
    {
        private static SitButton _instance;

        internal static SitButton Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(SitButton)} was never constructed");
                }
                return _instance;
            }
        }

        public bool ToggleButtonInsideMenu => false;

        public bool sitting = false;
        bool sitAwait = false;
        public bool canSit = true;

        public GameObject knightSit;
        public static SitButtonSettings settings = new SitButtonSettings();
        SitButtonMonoBehaviour behaviour;
        PlayMakerFSM HUDCanvas;

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public SitButton() : base("Sit Button")
        {
            _instance = this;
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("Room_nailmaster", "Sit Region/Knight Sit")
            };
        }

        // if you need preloads, you will need to implement GetPreloadNames and use the other signature of Initialize.
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            // put additional initialization logic here
            Log("Getting preloaded objects");
            knightSit = preloadedObjects["Room_nailmaster"]["Sit Region/Knight Sit"];

            behaviour = new GameObject("SitButtonMono").AddComponent<SitButtonMonoBehaviour>();
            UnityEngine.Object.DontDestroyOnLoad(behaviour.gameObject); //i'm still surprised this is possible in 1 command


            Log("Modifying Knight Sit object");
            UnityEngine.Object.DontDestroyOnLoad(knightSit);
            knightSit.GetComponent<tk2dSpriteAnimator>().AnimationCompleted += KnightSitAnimationComplete;


            Log("Adding hooks");
            ModHooks.HeroUpdateHook += HeroUpdate;
            ModHooks.AfterTakeDamageHook += OnDamage;
            SceneManagement.SceneManager.activeSceneChanged += activeSceneChanged;
            On.PlayMakerFSM.OnEnable += FSMEnable;

            Log("Initialized");
        }

        private void FSMEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);

            if (self.FsmName == "Slide Out" && self.name == "Hud Canvas")
            {
                Log("Found hud canvas");
                HUDCanvas = self;
            }/*else if (self.FsmName == "Prompt Control")
            {
                Log("Found prompt");
                prompts.Add(self);
            }*/
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) //put your helmets on, we're entering spaghetti code territory
        {
            MenuBuilder menuBuilder = new MenuBuilder("Sit Button");

            AnchoredPosition start = new AnchoredPosition();
            start.ParentAnchor = new Vector2(0, 1);
            start.ChildAnchor = new Vector2(0, 1);
            start.Offset = new Vector2(0, 0);

            AnchoredPosition contentPosition = new AnchoredPosition();
            contentPosition.ParentAnchor = new Vector2(0.5f, 1);
            contentPosition.ChildAnchor = new Vector2(0.5f, 1);
            contentPosition.Offset = new Vector2(0, -400);

            MenuButtonConfig backConfig = new MenuButtonConfig();
            backConfig.Style = MenuButtonStyle.VanillaStyle;
            backConfig.Label = "Back";
            backConfig.Proceed = true;
            backConfig.SubmitAction = m => ReturnToMenu(modListMenu);
            backConfig.CancelAction = m => ReturnToMenu(modListMenu);

            AnchoredPosition controlPosition = new AnchoredPosition();
            controlPosition.ParentAnchor = new Vector2(0.5f, 0);
            controlPosition.ChildAnchor = new Vector2(0.5f, 0);
            controlPosition.Offset = Vector2.zero;

            KeybindConfig sitConfig = new KeybindConfig();
            sitConfig.Style = KeybindStyle.VanillaStyle;
            sitConfig.Label = "Sit key";
            sitConfig.CancelAction = m => ReturnToMenu(modListMenu);

            HorizontalOptionConfig hideUIConfig = new HorizontalOptionConfig();
            hideUIConfig.Options = new string[] { "Off", "On" };
            hideUIConfig.Description = new DescriptionInfo() { Style = DescriptionStyle.HorizOptionSingleLineVanillaStyle, Text = "Enable to hide the HUD when you sit down" };
            hideUIConfig.Style = HorizontalOptionStyle.VanillaStyle;
            hideUIConfig.Label = "Hide HUD";
            hideUIConfig.ApplySetting = (self, index) => settings.hideHUD = index == 1;
            hideUIConfig.RefreshSetting = (self, apply) => self.optionList.SetOptionTo(settings.hideHUD ? 1 : 0);
            hideUIConfig.CancelAction = m => ReturnToMenu(modListMenu);

            /*HorizontalOptionConfig hidePromptConfig = new HorizontalOptionConfig();
            hidePromptConfig.Options = new string[] { "Off", "On" };
            hidePromptConfig.Description = new DescriptionInfo() { Style = DescriptionStyle.HorizOptionSingleLineVanillaStyle, Text = "Enable to hide text prompts when you sit down" };
            hidePromptConfig.Style = HorizontalOptionStyle.VanillaStyle;
            hidePromptConfig.Label = "Hide Prompts";
            hidePromptConfig.ApplySetting = (self, index) => settings.hidePrompt = index == 1;
            hidePromptConfig.RefreshSetting = (self, apply) => self.optionList.SetOptionTo(settings.hidePrompt ? 1 : 0);
            hidePromptConfig.CancelAction = m => ReturnToMenu(modListMenu);*/

            RegularGridLayout contentLayout = RegularGridLayout.CreateVerticalLayout(100);

            UnityEngine.UI.MenuOptionHorizontal[] options = new UnityEngine.UI.MenuOptionHorizontal[1];

            menuBuilder.CreateTitle("Sit Button", MenuTitleStyle.vanillaStyle);
            menuBuilder.CreateContentPane(RectTransformData.FromSizeAndPos(new RelVector2(new Vector2(500, 400)), contentPosition));
            menuBuilder.CreateControlPane(RectTransformData.FromSizeAndPos(new RelVector2(new Vector2(500, 400)), controlPosition));
            menuBuilder.AddContent(contentLayout, c => c.AddKeybind("Sit key", settings.sitAction.sit, sitConfig));
            menuBuilder.AddContent(contentLayout, c => c.AddHorizontalOption("hideUI", hideUIConfig, out options[0]));
            //menuBuilder.AddContent(contentLayout, c => c.AddHorizontalOption("hidePrompt", hidePromptConfig, out options[1]));
            menuBuilder.AddControls(new SingleContentLayout(new Vector2(0.5f, 0.5f)), c => c.AddMenuButton("Back", backConfig));

            foreach (UnityEngine.UI.MenuOptionHorizontal option in options)
            {
                option.menuSetting.RefreshValueFromGameSettings();
            }

            return menuBuilder.Build();
        }
        
        private void ReturnToMenu(MenuScreen modListMenu) => UIManager.instance.UIGoToDynamicMenu(modListMenu);

        private void activeSceneChanged(SceneManagement.Scene prevScene, SceneManagement.Scene newScene)
        {
            sitting = false;
            canSit = true;
            sitAwait = false;
            knightSit.GetComponent<tk2dSpriteAnimator>().StopAndResetFrame();

            if (HeroController.instance != null)
            {
                HeroController.instance.GetComponent<MeshRenderer>().enabled = true;
                knightSit.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        private int OnDamage(int hazardType, int damageAmount)
        {
            if (sitting)
            {
                RiseInstant();
            }

            return damageAmount;
        }

        private void KnightSitAnimationComplete(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
        {
            if (!sitAwait) return;
            sitAwait = false;

            if (sitting)
            {
                //this actually never happens, which is :( so this is moved to a coroutine
            }else
            {
                RiseLate();
            }
        }

        private void HeroUpdate()
        {
            if (!sitting && canSit && CanInspect() && settings.sitAction.sit.WasPressed)
            {
                Sit();
            }
            else if (sitting && canSit && RiseButtonDown())
            {
                Rise();
            }
            if (sitting || sitAwait)
            {
                knightSit.transform.position = HeroController.instance.transform.position;
            }
        }

        public void Sit() //brought to you by the fsm in matos room
        {
            HeroController hero = HeroController.instance;

            //take control
            hero.RelinquishControl();
            hero.StopAnimationControl();

            //sit
            Vector2 heroPos = hero.transform.position;
            if (settings.hideHUD) {
                HUDCanvas?.SendEvent("OUT");
            }
            /*if (settings.hidePrompt)
            {
                List<PlayMakerFSM> newPrompts = prompts;
                foreach (PlayMakerFSM prompt in newPrompts)
                {
                    if (prompt == null)
                    {
                        prompts.Remove(prompt);
                        continue;
                    }
                    prompt.SendEvent("DOWN");
                }
            }*/
            knightSit.SetActive(true);
            knightSit.transform.position = heroPos;
            knightSit.transform.SetScaleX(hero.transform.GetScaleX());
            knightSit.GetComponent<MeshRenderer>().enabled = true;
            hero.GetComponent<MeshRenderer>().enabled = false;
            knightSit.GetComponent<tk2dSpriteAnimator>().Play("Knight Sit");

            sitting = true;
            canSit = false;
            behaviour.StartCoroutine(SitLate());
        }
        IEnumerator<WaitForSeconds> SitLate()
        {

            yield return new WaitForSeconds(0.5f);

            canSit = true;
        }
        public void Rise() //brought to you by the fsm in matos room
        {
            HeroController hero = HeroController.instance;

            //rise
            knightSit.GetComponent<tk2dSpriteAnimator>().Play("Knight Sit End");
            if (settings.hideHUD)
            {
                HUDCanvas?.SendEvent("IN");
            }

            sitting = false;
            canSit = false;
            sitAwait = true;

            
        }
        private void RiseLate()
        {
            HeroController hero = HeroController.instance;

            //mesh rend
            knightSit.GetComponent<MeshRenderer>().enabled = false;
            hero.GetComponent<MeshRenderer>().enabled = true;

            //regain control
            hero.RegainControl();
            hero.StartAnimationControl();
            hero.playerData.SetBool("disablePause", false);

            canSit = true;
        }
        public void RiseInstant()
        {
            HeroController hero = HeroController.instance;

            if (settings.hideHUD) { } //todo later

            //mesh rend
            knightSit.GetComponent<MeshRenderer>().enabled = false;
            hero.GetComponent<MeshRenderer>().enabled = true;

            //regain control
            hero.RegainControl();
            hero.StartAnimationControl();
            hero.playerData.SetBool("disablePause", false);

            canSit = true;
        }

        public bool RiseButtonDown()
        {
            if (InputHandler.Instance.inputActions.up.IsPressed) return true;
            if (InputHandler.Instance.inputActions.down.IsPressed) return true;
            if (InputHandler.Instance.inputActions.left.IsPressed) return true;
            if (InputHandler.Instance.inputActions.right.IsPressed) return true;
            if (InputHandler.Instance.inputActions.jump.IsPressed) return true;
            if (InputHandler.Instance.inputActions.dash.IsPressed) return true;
            if (InputHandler.Instance.inputActions.attack.IsPressed) return true;

            return false;
        }
        public bool CanInspect()
        {
            HeroController hero = HeroController.instance;

            string[] negativeStates = new string[]
            {
                "attacking",
                "upAttacking",
                "downAttacking",
                "dashing",
                "backDashing"
            };
            foreach(string state in negativeStates)
            {
                if (hero.GetState(state)) return false;
            }


            return hero.acceptingInput && hero.GetState("onGround");
        }

        public void OnLoadGlobal(SitButtonSettings s)
        {
            settings = s;
        }

        public SitButtonSettings OnSaveGlobal() => settings;
    }
}
