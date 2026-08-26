using GorillaLocomotion;
using Oculus.Interaction;
using Photon.Pun;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using ShibaGTGenesisReborn.Mods.Custom;
using UnityEngine;
using static ShibaGTGenesisReborn.Settings;

namespace ShibaGTGenesisReborn.Menu
{
    internal class Buttons
    {
        public static ButtonInfo[][] buttons = new ButtonInfo[][]
        {
            new ButtonInfo[]
            { // Main Mods [ ALWAYS KEEP THIS FIRST NO MATTER WHAT ]
                new ButtonInfo { buttonText = "Save", method =() => Preferences.Save(), isTogglable = false, toolTip = "Save settings", enabled = false},
                new ButtonInfo { buttonText = "Enabled Mods", method =() => SettingsMods.enablemods(), isTogglable = false, toolTip = "View active mods"},
                new ButtonInfo { buttonText = "Favourite", method =() => SettingsMods.favouritemods(), isTogglable = false, toolTip = "View favorites"},
                new ButtonInfo { buttonText = "Room", method =() => SettingsMods.room(), isTogglable = false, toolTip = "Room mods"},
                new ButtonInfo { buttonText = "Advantages", method =() => SettingsMods.advantages(), isTogglable = false, toolTip = "Advantage mods"},
                new ButtonInfo { buttonText = "Movement", method =() => SettingsMods.movement(), isTogglable = false, toolTip = "Movement mods"},
                new ButtonInfo { buttonText = "Rig", method =() => SettingsMods.rig(), isTogglable = false, toolTip = "Rig mods"},
                new ButtonInfo { buttonText = "Fun", method =() => SettingsMods.fun(), isTogglable = false, toolTip = "Fun mods"},
                new ButtonInfo { buttonText = "Visual", method =() => SettingsMods.visuals(), isTogglable = false, toolTip = "Visual mods"},
                new ButtonInfo { buttonText = "Projectiles", method =() => SettingsMods.master(), isTogglable = false, toolTip = "Projectile mods"},
                new ButtonInfo { buttonText = "Overpowered", method =() => SettingsMods.overpowered(), isTogglable = false, toolTip = "OP mods"},
            },

            new ButtonInfo[]
            { // Settings [0]
                new ButtonInfo { buttonText = "Menu", method =() => SettingsMods.safety(), toolTip = "Menu settings", isTogglable = false},
                new ButtonInfo { buttonText = "Movement", method =() => SettingsMods.moveset(), toolTip = "Move settings", isTogglable = false},
                new ButtonInfo { buttonText = "Projectiles", method =() => SettingsMods.projset(), toolTip = "Proj settings", isTogglable = false},
                new ButtonInfo { buttonText = "Anti Report", method =() => mods.AntiReport(), toolTip = "Block reports", isTogglable = true, enabled = true},
                new ButtonInfo { buttonText = "Disable Quitbox", enableMethod = () => mods.disableQuitbox = true, disableMethod = () => mods.disableQuitbox = false, isTogglable = true, enabled = true, toolTip = "Prevent quitbox triggers from quitting the game"},
                new ButtonInfo { buttonText = "Filled ESP", enableMethod =() => mods.filledESP = true, disableMethod =() => mods.filledESP = false, isTogglable = true, enabled = false, toolTip = "Toggle filled 2D and 3D box ESP"},
            },

            new ButtonInfo[]
            { // Room [1]
                new ButtonInfo { buttonText = "RPC Protection", method =() => mods.RPCProt(false), enabled = false, isTogglable = false, toolTip = "RPC Protection"},
                new ButtonInfo { buttonText = "Experimental RPC Protection", method =() => mods.RPCProt(true), enabled = false, isTogglable = false, toolTip = "Experimental RPC Protection"},
                new ButtonInfo { buttonText = "Disconnect", method =() => { NetworkSystem.Instance.ReturnToSinglePlayer(); PhotonNetwork.Disconnect(); }, enabled = false, isTogglable = false, toolTip = "Leave room"},
                new ButtonInfo { buttonText = "B Disconnect", method =() => mods.BDisconnect(), enabled = false, isTogglable = true, toolTip = "Press B to leave"},
                new ButtonInfo { buttonText = "Join Genesis", method =() => mods.Joincodegenesis(), enabled = false, isTogglable = false, toolTip = "Join Genesis"},
                new ButtonInfo { buttonText = "Join Random Room", method =() => mods.JoinRandom(), enabled = false, isTogglable = false, toolTip = "Join random"},
                new ButtonInfo { buttonText = "Create Room", method =() => mods.CreateRoom(), enabled = false, isTogglable = false, toolTip = "Create a public room"},
                new ButtonInfo { buttonText = "Connect to Fastest Region", method =() => PhotonNetwork.ConnectToBestCloudServer(), enabled = false, isTogglable = false, toolTip = "Join US Central server"},
                new ButtonInfo { buttonText = "Connect to US Central", method =() => mods.ConnectToRegion("us"), enabled = false, isTogglable = false, toolTip = "Join US Central server"},
                new ButtonInfo { buttonText = "Connect to US West", method =() => mods.ConnectToRegion("usw"), enabled = false, isTogglable = false, toolTip = "Join US West server"},
                new ButtonInfo { buttonText = "Connect to EU", method =() => mods.ConnectToRegion("eu"), enabled = false, isTogglable = false, toolTip = "Join EU server"},
                new ButtonInfo { buttonText = "Mute Gun (CS)", method =() => mods.MuteGun(), isTogglable = true, toolTip = "Shoot to mute player"},
                new ButtonInfo { buttonText = "Mute All (CS)", method =() => mods.MuteAll(), enabled = false, isTogglable = false, toolTip = "Mute all players"},
                new ButtonInfo { buttonText = "Unmute Gun", method =() => mods.UnmuteGun(), isTogglable = true, toolTip = "Shoot to unmute player"},
                new ButtonInfo { buttonText = "Unmute All", method =() => mods.UnmuteAll(), enabled = false, isTogglable = false, toolTip = "Unmute all players"},
                new ButtonInfo { buttonText = "Priority Voice Gun", method =() => mods.PriorityVoiceGun(), disableMethod =() => mods.PriorityVoiceDisable(), isTogglable = true, toolTip = "Shoot players to prioritize/highlight their voice for you"},
                new ButtonInfo { buttonText = "Loud Voice All (CS)", method =() => mods.LoudVoiceAll(), disableMethod =() => mods.DisableLoudVoiceAll(), isTogglable = true, toolTip = "Make all player voices 2D and heard globally"},
                new ButtonInfo { buttonText = "Report Gun", method =() => mods.ReportGun(), isTogglable = true, toolTip = "Shoot to report player"},
                new ButtonInfo { buttonText = "Report All", method =() => mods.ReportAll(), enabled = false, isTogglable = false, toolTip = "Report all players"},
                new ButtonInfo { buttonText = "Copy Identity", method =() => mods.CopyPlayerIdentity(), isTogglable = true, toolTip = "Shoot player to copy name and color"},
                new ButtonInfo { buttonText = "Lobby Hop", method =() => mods.LobbyHop(), isTogglable = false, toolTip = "Disconnect and join new random room"},
                new ButtonInfo { buttonText = "Rejoin Room", method =() => mods.RejoinRoom(), isTogglable = false, toolTip = "Reconnect to current room"},
            },

            new ButtonInfo[]
            { // Advantages [2]
                new ButtonInfo { buttonText = "Tag Gun", method =() => mods.TagGun(), isTogglable = true, toolTip = "Shoot tags"},
                new ButtonInfo { buttonText = "Tag All", method =() => mods.TagAll(), isTogglable = true, toolTip = "Tags everyone"},
                new ButtonInfo { buttonText = "Tag Self", method =() => mods.TagSelf(), isTogglable = false, toolTip = "Tag yourself"},
                new ButtonInfo { buttonText = "Tag Aura", method =() => mods.TagAura(), isTogglable = true, toolTip = "Auto tag nearby uninfected monkeys"},
                new ButtonInfo { buttonText = "Tag Assist", method =() => mods.TagAssist(), disableMethod =() => mods.tagAssistTarget = null, isTogglable = true, toolTip = "Blatantly snaps hands and pulls you towards nearest uninfected monkey"},
                new ButtonInfo { buttonText = "No Tag On Join", method =() => mods.NoTagOnJoin(), isTogglable = true, toolTip = "No tag when joining"},
                new ButtonInfo { buttonText = "No Leaves", method =() => mods.removeleaves(), disableMethod =() => mods.addleaves(), isTogglable = true, toolTip = "Remove leaves"},
                new ButtonInfo { buttonText = "45 FPS", method =() => mods.FPS(45), isTogglable = true, toolTip = "Set 45 FPS"},
                new ButtonInfo { buttonText = "60 FPS", method =() => mods.FPS(60), isTogglable = true, toolTip = "Set 60 FPS"},
                new ButtonInfo { buttonText = "90 FPS", method =() => mods.FPS(90), isTogglable = true, toolTip = "Set 90 FPS"},
                new ButtonInfo { buttonText = "120 FPS", method =() => mods.FPS(120), isTogglable = true, toolTip = "Set 120 FPS"},
                new ButtonInfo { buttonText = "Unlock fps", method =() => { Application.targetFrameRate = int.MaxValue; QualitySettings.vSyncCount = 0; }, disableMethod =() => Application.targetFrameRate = 144, isTogglable = true, enabled = false, toolTip = "Unlocks FPS (doesnt work if nvidia control panel is limiting)" },
                new ButtonInfo { buttonText = "No Tag Freeze", method =() => mods.NoTagFreeze(), isTogglable = true, toolTip = "Remove tag freeze and slowdown"},
            },

            new ButtonInfo[]
            { // Movement [3]
                new ButtonInfo { buttonText = "Platforms", method =() => mods.Platforms(), isTogglable = true, toolTip = "Spawn platforms on trigger/grip"},
                new ButtonInfo { buttonText = "Invis Platforms", method =() => mods.Platforms(true), isTogglable = true, toolTip = "Spawn invisible platforms"},
                new ButtonInfo { buttonText = "Noclip (RT)", method =() => mods.Noclip(), isTogglable = true, toolTip = "Hold right trigger to phase through walls"},
                new ButtonInfo { buttonText = "Fly (B)", method =() => mods.CarMonkeyandfly(15f, true), isTogglable = true, toolTip = "Hold B to fly where you look"},
                new ButtonInfo { buttonText = "WASD Fly", method =() => mods.WASDFly(), isTogglable = true, toolTip = "Fly and look around with WASD/mouse"},
                new ButtonInfo { buttonText = "Car Monkey (A)", method =() => mods.CarMonkeyandfly(15f, false), isTogglable = true, toolTip = "Hold A to drive forward"},
                new ButtonInfo { buttonText = "TP Gun", method =() => mods.TeleportGun(), isTogglable = true, toolTip = "Point and shoot to teleport"},
                new ButtonInfo { buttonText = "Pull Mods", method =() => mods.PullMod(), isTogglable = true, toolTip = "just pull mod"},
                new ButtonInfo { buttonText = "Low Gravity", method =() => mods.GravityManager(mods.Gravitytypes.Low), isTogglable = true, toolTip = "Lowers gravity."},
                new ButtonInfo { buttonText = "High Gravity", method =() => mods.GravityManager(mods.Gravitytypes.High), isTogglable = true, toolTip = "Increases gravity."},
                new ButtonInfo { buttonText = "Zero Gravity", method =() => mods.GravityManager(mods.Gravitytypes.Zero), isTogglable = true, toolTip = "Removes gravity."},
                new ButtonInfo { buttonText = "Reverse Gravity", method =() => mods.GravityManager(mods.Gravitytypes.Reverse), disableMethod = () => mods.Reset_upsidedown(), isTogglable = true, toolTip = "Reverses gravity."},
                new ButtonInfo { buttonText = "Up And Down", method =() => mods.UpAndDown(), isTogglable = true, toolTip = "RT to fly up, LT to fly down"},
                new ButtonInfo { buttonText = "Slip Slap", method =() => mods.SlipSlap(), disableMethod =() => mods.UnSlipSlap(), isTogglable = true, toolTip = "Its just slip slap"},
                new ButtonInfo { buttonText = "No Slip", method =() => mods.NoSlip(), disableMethod =() => mods.ReSlip(), isTogglable = true, toolTip = "Disable all slippery surfaces"},
                new ButtonInfo { buttonText = "CheckPoint", method =() => mods.CheckPoint(), disableMethod =() => mods.CheckPointDisable(), isTogglable = true, toolTip = "RG to set checkpoint, A to teleport"},
                new ButtonInfo { buttonText = "Legit Slide Control", method =() => mods.SlideControl(0.05f), disableMethod =() => mods.SlideControl(0.00425f), isTogglable = true, toolTip = "Slightly more slide control"},
                new ButtonInfo { buttonText = "Blatant Slide Control", method =() => mods.SlideControl(0.08f), disableMethod =() => mods.SlideControl(0.00425f), isTogglable = true, toolTip = "High slide control"},
                new ButtonInfo { buttonText = "Grappling Hook", method =() => mods.GrapplingHook(), disableMethod =() => mods.GrapplingHookDisable(), isTogglable = true, toolTip = "Aim and pull with grappling hook"},
                new ButtonInfo { buttonText = "Air Swim", method =() => mods.AirSwim(), disableMethod =() => mods.AirSwimDisable(), isTogglable = true, toolTip = "Swim through the air"},
                new ButtonInfo { buttonText = "Jesus Monke", method =() => mods.JesusMonke(), disableMethod =() => mods.JesusMonkeDisable(), isTogglable = true, toolTip = "Walk and slide on water surfaces"},
                new ButtonInfo { buttonText = "Zipline Speed", method =() => mods.ZiplineSpeed(35f), disableMethod =() => mods.ZiplineSpeed(10f), isTogglable = true, toolTip = "Increase zipline speed"},
                new ButtonInfo { buttonText = "Catapult", method =() => mods.Catapult(), isTogglable = true, toolTip = "Shoot pointer to launch yourself"},
                new ButtonInfo { buttonText = "Sticky Hands", method =() => mods.StickyHands(), disableMethod =() => mods.ResetStickyHands(), isTogglable = true, toolTip = "Hold grip on surfaces to stick"},
                new ButtonInfo { buttonText = "PiggyBack", method =() => mods.PiggyBack(), disableMethod =() => mods.PiggyBackDisable(), isTogglable = true, toolTip = "Ride on another player's back"},
                new ButtonInfo { buttonText = "Follow Player", method =() => mods.FollowPlayer(), disableMethod =() => mods.FollowPlayerDisable(), isTogglable = true, toolTip = "Always follow slightly behind a player"},
                new ButtonInfo { buttonText = "Ender Pearl", method =() => mods.EnderPearl(), disableMethod =() => mods.EnderPearlDisable(), isTogglable = true, toolTip = "Grip to hold pearl, release to throw and teleport"},
                new ButtonInfo { buttonText = "Zipline Gun", method =() => mods.ZiplineGun(), disableMethod =() => mods.ZiplineGunDisable(), isTogglable = true, toolTip = "Shoot to create rideable zipline"},
            },

            new ButtonInfo[]
            { // Rig [4]
                new ButtonInfo { buttonText = "CosmetX", enableMethod =() => mods.EnableCosmetX(), disableMethod =() => mods.DisableCosmetX(), isTogglable = true, toolTip = "Unlock all cosmetics and network to other users" },
                new ButtonInfo { buttonText = "Ghost Monkey", method =() => mods.GhostMonke(), isTogglable = true, toolTip = "Freeze your rig"},
                new ButtonInfo { buttonText = "Invis Monkey", method =() => mods.InvisMonke(), isTogglable = true, toolTip = "Invisible monkey"},
                new ButtonInfo { buttonText = "Long Arms", method =() => mods.LongArms(), disableMethod =() => mods.NormalArms(), isTogglable = true, toolTip = "Long arms"},
                new ButtonInfo { buttonText = "No Fingers", method =() => mods.NoFinger(), isTogglable = true, toolTip = "No fingers"},
                new ButtonInfo { buttonText = "Spaz Rig", method =() => mods.SpazRig(), isTogglable = true, toolTip = "Spazzy monkey"},
                new ButtonInfo { buttonText = "Upside Down Head", method =() => VRRig.LocalRig.head.trackingRotationOffset.z = 180f, disableMethod =() => mods.FixHead(), isTogglable = true, toolTip = "neck upsidedown"},
                new ButtonInfo { buttonText = "Broken Neck", method =() => VRRig.LocalRig.head.trackingRotationOffset.z = 90f, disableMethod =() => mods.FixHead(), isTogglable = true, toolTip = "broken neck"},
                new ButtonInfo { buttonText = "Backwards Head", method =() => VRRig.LocalRig.head.trackingRotationOffset.y = 180f, disableMethod =() => mods.FixHead(), isTogglable = true, toolTip = "backwards head"},
                new ButtonInfo { buttonText = "Head Spinner", method =() => mods.HeadSpinner(), disableMethod =() => mods.FixHead(), isTogglable = true, toolTip = "Spin head continuously"},
            },

            new ButtonInfo[]
            { // fun [5]
                new ButtonInfo { buttonText = "Board Spam", method =() => mods.HoverboardSpam(), isTogglable = true, toolTip = "Hold RG to spam hoverboards"},
                new ButtonInfo { buttonText = "Loud Microphone", method =() => mods.LoudMicrophone(), disableMethod =() => mods.ResetMicrophoneVolume(), isTogglable = true, toolTip = "Boost microphone volume (25x)"},
                new ButtonInfo { buttonText = "Earrape Mic", method =() => mods.LoudMicrophone(25f), disableMethod =() => mods.ResetMicrophoneVolume(), isTogglable = true, toolTip = "Extreme microphone volume boost (100x)"},
                new ButtonInfo { buttonText = "Mute Microphone", method =() => mods.MuteMicrophone(), disableMethod =() => mods.UnmuteMicrophone(), isTogglable = true, toolTip = "Mute local microphone transmission"},
                new ButtonInfo { buttonText = "Microphone Echo", method =() => mods.MicrophoneEcho(true), disableMethod =() => mods.MicrophoneEcho(false), isTogglable = true, toolTip = "Echo your voice for other players"},
                new ButtonInfo { buttonText = "Chipmunk Mic", method =() => mods.SetMicrophonePitch(1.6f), disableMethod =() => mods.ResetMicrophonePitch(), isTogglable = true, toolTip = "High pitch voice modulation"},
                new ButtonInfo { buttonText = "Deep Voice Mic", method =() => mods.SetMicrophonePitch(0.6f), disableMethod =() => mods.ResetMicrophonePitch(), isTogglable = true, toolTip = "Deep voice modulation"},
                new ButtonInfo { buttonText = "Robot Mic", enableMethod =() => mods.robotMic = true, disableMethod =() => mods.robotMic = false, isTogglable = true, toolTip = "Metallic robotic voice modulation"},
                new ButtonInfo { buttonText = "Radio Mic", enableMethod =() => mods.radioMic = true, disableMethod =() => mods.radioMic = false, isTogglable = true, toolTip = "Tactical walkie-talkie radio voice"},
                new ButtonInfo { buttonText = "8-Bit Mic", enableMethod =() => mods.bitcrushMic = true, disableMethod =() => mods.bitcrushMic = false, isTogglable = true, toolTip = "Retro 8-bit arcade bitcrusher voice"},
                new ButtonInfo { buttonText = "Underwater Mic", enableMethod =() => mods.underwaterMic = true, disableMethod =() => mods.underwaterMic = false, isTogglable = true, toolTip = "Muffled underwater voice filter"},
                new ButtonInfo { buttonText = "Stutter Mic", enableMethod =() => mods.stutterMic = true, disableMethod =() => mods.stutterMic = false, isTogglable = true, toolTip = "Choppy tremolo stutter mic effect"},
                new ButtonInfo { buttonText = "Fix Microphone", method =() => mods.FixMicrophone(), isTogglable = false, toolTip = "Reset and repair microphone settings"},
                new ButtonInfo { buttonText = "Hear Self", method =() => mods.HearSelf(true), disableMethod =() => mods.HearSelf(false), isTogglable = true, toolTip = "Hear your own microphone live to test audio"},
                new ButtonInfo { buttonText = "Noise Cancellation", method =() => mods.NoiseCancellation(), disableMethod =() => mods.DisableNoiseCancellation(), isTogglable = true, toolTip = "Gate out background noise via VAD threshold"},
                new ButtonInfo { buttonText = "Waterbend", method =() => mods.WaterSplash(), isTogglable = true, toolTip = "Splash water around hands"},
                new ButtonInfo { buttonText = "Splash Gun", method =() => mods.SplashGun(), isTogglable = true, toolTip = "Shoot water splashes at pointer"},
                new ButtonInfo { buttonText = "Get Bracelet", method =() => mods.GetBracelet(), isTogglable = false, toolTip = "Equip right hand friendship bracelet"},
                new ButtonInfo { buttonText = "Remove Bracelet", method =() => mods.RemoveBracelet(), isTogglable = false, toolTip = "Remove all friendship bracelets"},
                new ButtonInfo { buttonText = "Get Left Bracelet", method =() => mods.GetLeftBracelet(), isTogglable = false, toolTip = "Equip left hand friendship bracelet"},
                new ButtonInfo { buttonText = "Get Dual Bracelets", method =() => mods.GetDualBracelets(), isTogglable = false, toolTip = "Equip bracelets on both hands"},
                new ButtonInfo { buttonText = "Bracelet Spam", method =() => mods.BraceletSpam(), disableMethod =() => mods.NoBracelet(), isTogglable = true, toolTip = "Spam friend bracelets"},
                new ButtonInfo { buttonText = "Dual Bracelet Spam", method =() => mods.DualBraceletSpam(), disableMethod =() => mods.NoBracelet(), isTogglable = true, toolTip = "Spam dual friend bracelets"},
                new ButtonInfo { buttonText = "Rainbow Bracelet", method =() => mods.RainbowBracelet(), disableMethod =() => mods.NoBracelet(), isTogglable = true, toolTip = "Rainbow animated friendship bracelet beads"},
                new ButtonInfo { buttonText = "Match Color Bracelet", method =() => mods.CustomColorBracelet(), disableMethod =() => mods.NoBracelet(), isTogglable = true, toolTip = "Match bracelet beads to monkey color"},
                new ButtonInfo { buttonText = "Gold Bracelet", method =() => mods.GoldBracelet(), disableMethod =() => mods.NoBracelet(), isTogglable = true, toolTip = "Golden friendship bracelet beads"},
                new ButtonInfo { buttonText = "Party With Room", method =() => mods.PartyWithRoom(), disableMethod =() => mods.NoBracelet(), isTogglable = true, toolTip = "Display friendship beads matching every player in the lobby"},
                new ButtonInfo { buttonText = "Networking Library", enableMethod = () => { if (NetworkingLibrary.Instance != null) NetworkingLibrary.Instance.NetworkEnabled = true; }, disableMethod = () => { if (NetworkingLibrary.Instance != null) NetworkingLibrary.Instance.NetworkEnabled = false; }, isTogglable = true, toolTip = "Toggle custom networking", enabled = NetworkingLibrary.Instance?.NetworkEnabled ?? true },
                new ButtonInfo { buttonText = "Boombox", method = () => BoomboxManager.BoomboxLoop("https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/boombox.obj", "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/boomboxmesh.png"), disableMethod = () => BoomboxManager.Kill(), isTogglable = true, toolTip = "Spawn boombox"},
                new ButtonInfo { buttonText = "Boombox Audios", method = () => SettingsMods.boomboxAudios(), isTogglable = false, toolTip = "Choose audio from Genesis/boombox folder"},
                new ButtonInfo { buttonText = "Soundboard", method = () => SettingsMods.soundboardAudios(), isTogglable = false, toolTip = "Play sounds through in-game microphone"},
                new ButtonInfo { buttonText = "Stop Soundboard", method = () => SoundboardManager.Stop(), isTogglable = false, toolTip = "Stop playing soundboard audio"},
                new ButtonInfo { buttonText = "Boombox Volume +", method = () => BoomboxManager.AdjustVolume(0.1f), isTogglable = false, toolTip = "Volume up"},
                new ButtonInfo { buttonText = "Boombox Volume -", method = () => BoomboxManager.AdjustVolume(-0.1f), isTogglable = false, toolTip = "Volume down"},
                new ButtonInfo { buttonText = "Boombox Speed +", method = () => BoomboxManager.AdjustPitchSpeed(0.1f), isTogglable = false, toolTip = "Faster song"},
                new ButtonInfo { buttonText = "Boombox Speed -", method = () => BoomboxManager.AdjustPitchSpeed(-0.1f), isTogglable = false, toolTip = "Slower song"},
                new ButtonInfo { buttonText = "Boombox Visualizer", enableMethod = () => BoomboxManager.UseVisualizer = true, disableMethod = () => BoomboxManager.UseVisualizer = false, isTogglable = true, enabled = BoomboxManager.UseVisualizer, toolTip = "Show visualizer"},
                new ButtonInfo { buttonText = "Visualizer Intensity +", method = () => BoomboxManager.VisualizerIntensity = Mathf.Clamp(BoomboxManager.VisualizerIntensity + 1f, 0f, 10f), isTogglable = false, toolTip = "Bigger bars"},
                new ButtonInfo { buttonText = "Visualizer Intensity -", method = () => BoomboxManager.VisualizerIntensity = Mathf.Clamp(BoomboxManager.VisualizerIntensity - 1f, 0f, 10f), isTogglable = false, toolTip = "Smaller bars"},
                new ButtonInfo { buttonText = "Visualizer Base Scale +", method = () => BoomboxManager.BaseScale = Mathf.Clamp(BoomboxManager.BaseScale + 1f, 0.1f, 10f), isTogglable = false, toolTip = "Wider bars"},
                new ButtonInfo { buttonText = "Visualizer Base Scale -", method = () => BoomboxManager.BaseScale = Mathf.Clamp(BoomboxManager.BaseScale - 1f, 0.1f, 10f), isTogglable = false, toolTip = "Narrower bars"},
                new ButtonInfo { buttonText = "Grosh Holdable", method = () => GroshHolder.GroshLoop("https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/Grosh.Holdable.obj", "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/iidktexture.png"), disableMethod = () => GroshHolder.Kill(), isTogglable = true, toolTip = "Hold Grosh"},
                new ButtonInfo { buttonText = "Maxwell Holdable", enableMethod = () => MaxwellHolder.DownloadAssets(), method = () => MaxwellHolder.CatLoop(), disableMethod = () => MaxwellHolder.Kill(), isTogglable = true, toolTip = "Hold Maxwell"},
                new ButtonInfo { buttonText = "Triple T Holdable", method = () => SusTung.TungShooter("https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/TungTungTungSahur.obj", "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/shaded.png", "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/tungtung.wav"), disableMethod = () => SusTung.Kill(), isTogglable = true, toolTip = "Hold Tung"},
                new ButtonInfo { buttonText = "Fat Seal Spammer", method = () => FatSealSpammer.SealLoop("https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/fatseal.obj", "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/fatseal.jpeg"), disableMethod = () => FatSealSpammer.Kill(), isTogglable = true, toolTip = "Spawn seals"},
                new ButtonInfo { buttonText = "Vape", method = () => Vape.InitVape("https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/juul.obj", "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/JUUL_BOI_Color.png"), disableMethod = () => Vape.Kill(), isTogglable = true, toolTip = "Hold vape"},
                new ButtonInfo { buttonText = "Stun Grenade (LOUD)", method = () => StunGrenadeManager.StunLoop(), disableMethod = () => StunGrenadeManager.Kill(), isTogglable = true, toolTip = "Press RG to hold grenade, release to throw (3s timer)"},
                new ButtonInfo { buttonText = "Bomb (LOUD)", method = () => BombManager.BombLoop(), disableMethod = () => BombManager.Kill(), isTogglable = true, toolTip = "Press RG to spawn a bomb (3s fuse)"},
            },

            new ButtonInfo[]
            { // visuals [6]
                new ButtonInfo { buttonText = "RGB Monke (stump)", method =() => mods.RGB(), isTogglable = true, toolTip = "Cycle player colors in stump"},
                new ButtonInfo { buttonText = "Strobe Monke (stump)", method =() => mods.RGB(true), isTogglable = true, toolTip = "Rapidly strobe player colors in stump"},
                new ButtonInfo { buttonText = "Tracers", method =() => mods.Tracers(), isTogglable = true, toolTip = "Draw lines to players"},
                new ButtonInfo { buttonText = "Beacon ESP", method =() => mods.BeaconESP(), isTogglable = true, toolTip = "Draw vertical beacon beams from players"},
                new ButtonInfo { buttonText = "Casual Chams", method =() => mods.CasualFullBodyESP(), disableMethod =() => mods.DisableFullBodyESP(), isTogglable = true, toolTip = "Highlight all players"},
                new ButtonInfo { buttonText = "Skeleton ESP", method =() => mods.SkeletonESP(), isTogglable = true, toolTip = "Draw bone skeletons on players"},
                new ButtonInfo { buttonText = "2D Box ESP", method =() => mods.TwoDBoxESP(), isTogglable = true, toolTip = "Draw 2D player boxes"},
                new ButtonInfo { buttonText = "3D Box ESP", method =() => mods.BoxESP(), isTogglable = true, toolTip = "Draw 3D player boxes"},
                new ButtonInfo { buttonText = "Infection Chams", method =() => mods.FullBodyESP(), disableMethod =() => mods.DisableFullBodyESP(), isTogglable = true, toolTip = "Highlight infected players"},
                new ButtonInfo { buttonText = "Infection Skeleton ESP", method =() => mods.InfectionSkeletonESP(), isTogglable = true, toolTip = "Draw infection colored skeletons"},
                new ButtonInfo { buttonText = "Infection 2D Box ESP", method =() => mods.InfectionTwoDBoxESP(), isTogglable = true, toolTip = "Draw infection colored 2D boxes"},
                new ButtonInfo { buttonText = "Infection 3D Box ESP", method =() => mods.InfectionBoxESP(), isTogglable = true, toolTip = "Draw infection colored 3D boxes"},
                new ButtonInfo { buttonText = "Name Tags", method =() => mods.NameAndDistanceTags(), isTogglable = true, toolTip = "Show player name and distance"},
                new ButtonInfo { buttonText = "cursedgtag", overlapText = "Cursed Mode: Off", method =() => mods.CursedGTAG(), isTogglable = false, toolTip = "Change cursed time override"},
                new ButtonInfo { buttonText = "Time Switcher", overlapText = "Time: Default", method =() => mods.TimeSwitcher(), isTogglable = false, toolTip = "Change time of day"},
                new ButtonInfo { buttonText = "Weather Switcher", overlapText = "Weather: Default", method =() => mods.CycleWeather(), isTogglable = false, toolTip = "Change weather mode (Rain/Clear)"},
            },

            new ButtonInfo[]
            { // Projectile [7]
                new ButtonInfo { buttonText = "Projectile Gun", method =() => mods.ProjectileGun(), isTogglable = true, toolTip = "Shoot projectile beam at pointer"},
                new ButtonInfo { buttonText = "Projectile Spam (B)", method =() => mods.SnowballSpam(GorillaLocomotion.GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0f), GTPlayer.Instance.RightHand.controllerTransform.position), isTogglable = true, toolTip = "Hold B to spam from hand"},
                new ButtonInfo { buttonText = "Snowball Launcher", method =() => mods.SnowballLauncher(), isTogglable = true, toolTip = "Press left or right grip to launch a snowball"},
                new ButtonInfo { buttonText = "Snowball Aimbot", method =() => mods.SnowballAimbot(), isTogglable = true, toolTip = "Auto-aim snowballs at nearest monkey"},
                new ButtonInfo { buttonText = "Snowball Orbit", method =() => mods.SnowballOrbit(), isTogglable = true, toolTip = "Orbiting shield of snowballs"},
                new ButtonInfo { buttonText = "Snowball Rain", method =() => mods.SnowballRain(), isTogglable = true, toolTip = "Rain snowballs from sky"},
                new ButtonInfo { buttonText = "Snowball Fling Gun", method =() => mods.FlingGun(), isTogglable = true, toolTip = "Fling locked target with giant snowballs"},
            },

            new ButtonInfo[]
            { // overpowered [8]
                new ButtonInfo { buttonText = "lagpwr", overlapText = "Lag Power: Weak", method =() => Main.Change("lagpwr", ref mods.lagindex, mods.lagnames), isTogglable = false, toolTip = "Lag target player with events"},
                new ButtonInfo { buttonText = "Lag Gun", method =() => mods.LagGun(), isTogglable = true, toolTip = "Lag target player with events"},
                new ButtonInfo { buttonText = "Lag All", method =() => mods.LagAll(), isTogglable = true, toolTip = "Lag all players in room"},
            },

            new ButtonInfo[]
            { // enbled mods [10]

            },

            new ButtonInfo[]
            { // favourite mods [11]

            },

            new ButtonInfo[]
            { // admin mods [12]

            },

            new ButtonInfo[]
            { // Boombox Audios [13]

            },

            new ButtonInfo[]
            { // Soundboard Audios [14]

            },

            new ButtonInfo[]
            { // Menu Settings
                new ButtonInfo { buttonText = "Left Hand", enableMethod =() => SettingsMods.LeftHand(), disableMethod =() => SettingsMods.RightHand(), toolTip = "Toggle menu hand", enabled = !rightHanded},
                new ButtonInfo { buttonText = "FPS Counter", enableMethod =() => SettingsMods.EnableFPSCounter(), disableMethod =() => SettingsMods.DisableFPSCounter(), enabled = fpsCounter, toolTip = "Show FPS counter"},
                new ButtonInfo { buttonText = "Setting Button", enableMethod =() => SettingsButton = true, disableMethod =() => SettingsButton = false, enabled = SettingsButton, toolTip = "Show settings button"},
                new ButtonInfo { buttonText = "Folder Button", enableMethod =() => FolderButton = true, disableMethod =() => FolderButton = false, enabled = FolderButton, toolTip = "Show folder button"},
                new ButtonInfo { buttonText = "Leave Button", enableMethod =() => SettingsMods.EnableDisconnectButton(), disableMethod =() => SettingsMods.DisableDisconnectButton(), enabled = disconnectButton, toolTip = "Show disconnect button"},
                new ButtonInfo { buttonText = "Remove All Prefs", method =() => Preferences.Reset(), isTogglable = false, enabled = false, toolTip = "Reset saved preferences"},
                new ButtonInfo { buttonText = "PPos", overlapText = "Menu Layout: ShibaGT", isTogglable = false, method =() => mods.SwitchPagePos(), enabled = false, toolTip = "Switch menu layout"},
                new ButtonInfo { buttonText = "Outline Menu", isTogglable = true, enableMethod =() => Main.showOutline = true, disableMethod =() => Main.showOutline = false, enabled = Main.showOutline, toolTip = "Toggle menu outline"},
                new ButtonInfo { buttonText = "Custom Button Audio", isTogglable = true, enableMethod =() => Button.customAudio = true, disableMethod =() => Button.customAudio = false, enabled = Button.customAudio, toolTip = "Use our custom button click audios"},
                new ButtonInfo { buttonText = "Cycle Button Audio", overlapText = "Click Audio: Sound 1", isTogglable = false, method = () => MenuAudio.CycleClickSound(), toolTip = "Cycle through 8 custom button click sounds"},
                new ButtonInfo { buttonText = "COC", overlapText = "Outline: Blue", isTogglable = false, method =() => Main.Change("COC", ref mods.OutlineIndex, mods.outnames, () => Main.outlineColor = mods.outlines[mods.OutlineIndex]), enabled = false, toolTip = "Cycle outline color"},
                new ButtonInfo { buttonText = "Streamer Mode", enableMethod =() => StreamerMode.Enable(), disableMethod =() => StreamerMode.Disable(), enabled = streamerMode, isTogglable = true, toolTip = "Hides menu and all mod visuals from recordings & spectator view"},
                new ButtonInfo { buttonText = "Panic Button", enableMethod =() => mods.EnablePanic(), disableMethod =() => mods.DisablePanic(), isTogglable = true, toolTip = "Disable all mods and disconnect safely"},
            },

            new ButtonInfo[]
            { // Move Set
                new ButtonInfo { buttonText = "pltclr", method =() => Main.Change("pltclr", ref mods.Platcolor, mods.ColorNames, () => mods.PlatColor = mods.PlatColors[mods.Platcolor]), isTogglable = false, overlapText = "Plat Color: Blue", toolTip = "Change platform color"},
                new ButtonInfo { buttonText = "pullmode", method =() => mods.ChangePullMode(), isTogglable = false, overlapText = "Pull Mode: Legit", toolTip = "Change pull mode"},
            },

            new ButtonInfo[]
            { // Proj Set
                new ButtonInfo{ buttonText = "Projectile Speed", overlapText = "Speed: Normal", method =() => Main.Change("Projectile Speed", ref mods.projectileSpeedIndex, mods.projectileSpeedNames), isTogglable = false, toolTip = "Cycle projectile firing speed"},
                new ButtonInfo{ buttonText = "Big Snowballs", enableMethod =() => mods.biig = true, disableMethod =() => mods.biig = false, isTogglable = true, toolTip = "Giant snowballs"},
                new ButtonInfo{ buttonText = "Rainbow Snowballs (CS)", enableMethod =() => mods.rainbowProjectiles = true, disableMethod =() => mods.rainbowProjectiles = false, isTogglable = true, toolTip = "Rainbow RGB snowballs"},
            },

            new ButtonInfo[]
            {
                new ButtonInfo { buttonText = "home", method =() => SettingsMods.ReturnHome(), isTogglable = false, toolTip = "Go back"},
            },
        };
    }
}