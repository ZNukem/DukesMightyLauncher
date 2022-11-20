#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.DataReaders;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.Games.HalfLife
{
	public class DNFHandler : GameHandler
	{
		#region ================= Variables

		private Dictionary<string, string> knowngamefolders; // Folder names and titles for official expansions, <rogue, MP2: Ground Zero>
		private List<VideoModeInfo> rmodes;
		private HashSet<string> nonengines; // HL comes with a lot of unrelated exes...

		#endregion

		#region ================= Properties

		public override string GameTitle => "DukeForever";

		#endregion

		#region ================= Setup

		// Valid Half-Life path if "valve\pak0.pak" exists, I guess...
		protected override bool CanHandle(string gamepath)
		{
			return File.Exists(Path.Combine(gamepath, "Maps\\Map00.dnf")); // HL GoldSource
		}

		// Data initialization order matters (horrible, I know...)!
		protected override void Setup(string gamepath)
		{
			// Default mod path
			defaultmodpath = Path.Combine(gamepath, "System").ToLowerInvariant();

			// Nothing to ignore
			ignoredmapprefix = string.Empty;

			// Demo extensions
			supporteddemoextensions.Add(".dem");

			// Setup map delegates
			getfoldermaps = DirectoryReader.GetMaps;

			foldercontainsmaps = DirectoryReader.ContainsMaps;

			getmapinfo = null; // HL maps contain no useful data

			// Setup fullscreen args...
			fullscreenarg[true] = "1";
			fullscreenarg[false] = "0";

			// Setup launch params
			launchparams[ItemType.ENGINE] = string.Empty;
			launchparams[ItemType.RESOLUTION] = "+fullscreen {1} +vid_mode {0}"; // "-sw -w {0} -h {1}"
			launchparams[ItemType.GAME] = string.Empty;
			launchparams[ItemType.MOD] = "{0}";
			launchparams[ItemType.MAP] = "{0}";
			launchparams[ItemType.SKILL] = "+skill {0}";
            launchparams[ItemType.CLASS] = string.Empty;

			// Setup skills (requires launchparams)
			skills.AddRange(new[]
			{
				new SkillItem("Piece Of Cake", "0"),
				new SkillItem("Lets Rock", "1", true),
				new SkillItem("Come Get Some", "2"),
				new SkillItem("Damn I'M Good", "3"),
			});

			// Setup known folders
			knowngamefolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "DLC\\DLC01", "Blue Shift" },
				{ "DLC\\DLC03", "The Doctor Who Clone Me" },
				{ "gearbox", "Opposing Forces" },
				{ "ricochet", "Ricochet" },
				{ "tfc", "Team Fortress Classic" },
			};

			// Setup fixed r_modes... Taken from engine\client\gl_vidnt.c (xash)
			int c = 0;
			rmodes = new List<VideoModeInfo>
			{
				new VideoModeInfo(640, 480, c++),
				new VideoModeInfo(800, 600, c++),
				new VideoModeInfo(960, 720, c++),
				new VideoModeInfo(1024, 768, c++),
				new VideoModeInfo(1152, 864, c++),
				new VideoModeInfo(1280, 960, c++),
				new VideoModeInfo(1280, 1024, c++),
				new VideoModeInfo(1600, 1200, c++),
				new VideoModeInfo(2048, 1536, c++),

				new VideoModeInfo(800, 480, c++),
				new VideoModeInfo(856, 480, c++),
				new VideoModeInfo(960, 540, c++),
				new VideoModeInfo(1024, 576, c++),
				new VideoModeInfo(1024, 600, c++),
				new VideoModeInfo(1280, 720, c++),
				new VideoModeInfo(1360, 768, c++),
				new VideoModeInfo(1366, 768, c++),
				new VideoModeInfo(1440, 900, c++),
				new VideoModeInfo(1680, 1050, c++),
				new VideoModeInfo(1920, 1080, c++),
				new VideoModeInfo(1920, 1200, c++),
				new VideoModeInfo(2560, 1440, c++),
				new VideoModeInfo(2560, 1600, c++),
				new VideoModeInfo(1600, 900, c++),
				new VideoModeInfo(3840, 2160, c++),
			};

			// Setup non-engines...
			nonengines = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"hlupdate",
				"opforup",
				"SierraUp",
				"upd",
				"UtDel32",
				"voice_tweak",
			};

			// Pass on to base...
			base.Setup(gamepath);
		}

		#endregion

		#region ================= Methods

		public override List<ResolutionItem> GetVideoModes()
		{
			return DisplayTools.GetFixedVideoModes(rmodes);
		}

		public override List<ModItem> GetMods()
		{
			var result = new List<ModItem>();

			foreach(string folder in Directory.GetDirectories(gamepath))
			{
				// Skip folder if it has no maps or client.dll
				if(!foldercontainsmaps(folder)) continue;

				string name = folder.Substring(gamepath.Length + 1);
				bool isbuiltin = (string.Compare(folder, defaultmodpath, StringComparison.OrdinalIgnoreCase) == 0);
				string title = (knowngamefolders.ContainsKey(name) ? knowngamefolders[name] : name);

				result.Add(new ModItem(title, name, folder, isbuiltin));
			}

			// Push known mods above regular ones
			result.Sort((i1, i2) =>
			{
				bool firstknown = (i1.Title != i1.Value);
				bool secondknown = (i2.Title != i2.Value);

				if(firstknown == secondknown) return string.Compare(i1.Title, i2.Title, StringComparison.Ordinal);
				return (firstknown ? -1 : 1);
			});

			return result;
		}

		protected override bool IsEngine(string filename)
		{
			return !nonengines.Contains(Path.GetFileNameWithoutExtension(filename)) && base.IsEngine(filename);
		}

		#endregion
	}
}
