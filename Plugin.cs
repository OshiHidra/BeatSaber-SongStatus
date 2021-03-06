using System;
using System.Globalization;
using System.IO;
using System.Linq;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SongStatus
{
	public class Plugin : IPlugin
	{
		private const string MenuSceneName = "Menu";
		private const string GameSceneName = "StandardLevel";
		
		private string _statusPath = Path.Combine(Environment.CurrentDirectory, "UserData/songStatus.txt");
		private readonly string _defaultTemplate = string.Join(
			Environment.NewLine,
			"Playing: {songName} {songSubName} - {authorName}",
			"{gamemode} {difficulty} | BPM: {beatsPerMinute}",
			"{[isNoFail]} {[modifiers]}");

		private StandardLevelSceneSetupDataSO _mainSetupData;
        private bool _init;
		private readonly string _templatePath = Path.Combine(Environment.CurrentDirectory, "UserData/songStatusTemplate.txt");
        private GameMode _gamemode = GameMode.Standard;
        private BeatmapCharacteristicSelectionViewController _beatmapCharacteristicSelectionViewController;

        public string Name
		{
			get { return "Song Status"; }
		}

		public string Version
		{
			get { return "v1.4.2"; }
		}

		public void OnApplicationStart()
		{
			if (_init) return;
			_init = true;
			SceneManager.sceneLoaded += OnSceneLoaded;
			
    		if (!File.Exists(_templatePath))
			{
				File.WriteAllText(_templatePath, _defaultTemplate);
			}
		}

		public void OnApplicationQuit()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			File.WriteAllText(_statusPath, string.Empty);
		}

		private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			string templateText;
			if (!File.Exists(_templatePath))
			{
				templateText = _defaultTemplate;
				File.WriteAllText(_templatePath, templateText);
			}
			else
			{
				templateText = File.ReadAllText(_templatePath);
			}

            if (newScene.name == MenuSceneName)
            {
                //Menu scene loaded
                File.WriteAllText(_statusPath, string.Empty);
                _beatmapCharacteristicSelectionViewController = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSelectionViewController>().FirstOrDefault();
                if (_beatmapCharacteristicSelectionViewController == null)
                    return;
                _beatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent += this.OnDidSelectBeatmapCharacteristicEvent;

            }
            else if (newScene.name != MenuSceneName)
            {
                _mainSetupData = Resources.FindObjectsOfTypeAll<StandardLevelSceneSetupDataSO>().FirstOrDefault();
                if (_mainSetupData == null)
                {
                    Console.WriteLine("Song Status: Error finding the scriptable objects required to update presence.");
                    return;
                } 

                //Main game scene loaded

                var diff = _mainSetupData.difficultyBeatmap;
                var song = diff.level;
                var mods = _mainSetupData.gameplayCoreSetupData.gameplayModifiers;
                var modsList = string.Empty;
                if (!mods.IsWithoutModifiers()) {
                    modsList += mods.instaFail ? "Instant Fail, " : string.Empty;
                    modsList += mods.batteryEnergy ? "Battery Energy, " : string.Empty;
                    modsList += mods.disappearingArrows ? "Disappearing Arrows, " : string.Empty;
                    modsList += mods.noBombs ? "No Bombs, " : string.Empty;
                    modsList += mods.noObstacles ? "No Walls, " : string.Empty;
                    modsList += mods.songSpeedMul != 1.0f ? "Speed " + mods.songSpeedMul + "x" : string.Empty;
                     
                    modsList = modsList.Trim(new char[] { ' ', ',' } );
                }

                var gameplayModeText = _gamemode == GameMode.OneSaber ? "One Saber" : _gamemode == GameMode.NoArrows ? "No Arrow" : "Standard";
                var keywords = templateText.Split('{', '}');
				
				templateText = ReplaceKeyword("songName", song.songName, keywords, templateText);
				templateText = ReplaceKeyword("songSubName", song.songSubName, keywords, templateText);
				templateText = ReplaceKeyword("authorName", song.songAuthorName, keywords, templateText);
				templateText = ReplaceKeyword("gamemode", gameplayModeText, keywords, templateText);
				templateText = ReplaceKeyword("difficulty", diff.difficulty.Name(), keywords, templateText);
                templateText = ReplaceKeyword("isNoFail",
					mods.noFail ? "No Fail" : string.Empty, keywords, templateText);
                templateText = ReplaceKeyword("modifiers", modsList, keywords, templateText);
                templateText = ReplaceKeyword("beatsPerMinute",
					song.beatsPerMinute.ToString(CultureInfo.InvariantCulture), keywords, templateText);
				templateText = ReplaceKeyword("notesCount",
					diff.beatmapData.notesCount.ToString(CultureInfo.InvariantCulture), keywords, templateText);
				templateText = ReplaceKeyword("obstaclesCount",
					diff.beatmapData.obstaclesCount.ToString(CultureInfo.InvariantCulture), keywords, templateText);

				File.WriteAllText(_statusPath, templateText);
			}
		}

		private string ReplaceKeyword(string keyword, string replaceKeyword, string[] keywords, string text)
		{
			if (!keywords.Any(x => x.Contains(keyword))) return text;
			var containingKeywords = keywords.Where(x => x.Contains(keyword));

			if (string.IsNullOrEmpty(replaceKeyword))
			{
				//If the replacement word is null or empty, we want to remove the whole bracket.
				foreach (var containingKeyword in containingKeywords)
				{
					text = text.Replace("{" + containingKeyword + "}", string.Empty);
				}

				return text;
			}

			foreach (var containingKeyword in containingKeywords)
			{
				text = text.Replace("{" + containingKeyword + "}", containingKeyword);
			}

			text = text.Replace(keyword, replaceKeyword);

			return text;
		}

		public void OnLevelWasLoaded(int level)
		{

		}

		public void OnLevelWasInitialized(int level)
		{

		}

		public void OnUpdate()
		{

		}

		public void OnFixedUpdate()
		{

		}

        private void OnDidSelectBeatmapCharacteristicEvent(BeatmapCharacteristicSelectionViewController viewController, BeatmapCharacteristicSO characteristic)
        {
            switch (characteristic.characteristicName)
            {
                case "No Arrows":
                    _gamemode = GameMode.NoArrows;
                    break;
                case "One Saber":
                    _gamemode = GameMode.OneSaber;
                    break;
                default:
                    _gamemode = GameMode.Standard;
                    break;
            }
        }
        private enum GameMode
        {
            Standard,
            OneSaber,
            NoArrows
        }
    }
}