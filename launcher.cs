using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Diagnostics;
using Internals;

public class ClientInfo {
	public static string[] GetVersions(string gamePath){
		try{
			List<string> versions = new List<string>();
			foreach(string directory in Directory.GetDirectories(gamePath + "\\versions")){
				foreach(string file in Directory.GetFiles(directory)){
					if(file.EndsWith(".jar")){
						versions.Add(directory.Replace(gamePath + "\\versions\\", ""));
						break;
					}
				}
			}
			return versions.ToArray();
		}catch{}
		return null;
	}
}


public class VersionLauncher {
	
	private LaunchConfig config;
	private string launchApp, launchArguments;
	private Dictionary<string, string> settings;

	public VersionLauncher(string jvmDir, string gameDir, string versionName, string playerName, int memory){
		this.config = new LaunchConfig(gameDir, gameDir + "\\versions\\", versionName);

		settings = new Dictionary<string, string>();
		settings["auth_player_name"] 	= playerName;
		settings["game_directory"] 	= gameDir;
		settings["assets_root"] 	= "\"" + gameDir + "\\assets\"";
		settings["assets_index_name"] 	= config.AssetIndex;
		settings["version"] 		= config.AssetIndex;
		settings["version_name"] 	= config.AssetIndex;
		settings["auth_uuid"] 		= "0000-0000-0000-0000";
		settings["auth_xuid"] 		= "0000-0000-0000-0000";
		settings["auth_access_token"] 	= "null";
		settings["clientid"] 		= "1";
		settings["user_type"] 		= "legacy";
		settings["version_type"] 	= config.Type;
		settings["natives_directory"] 	= "\"" + gameDir + "\\versions\\" + versionName + "\\natives\"";
		settings["launcher_name"] 	= "huskerdev_launcher";
		settings["launcher_version"] 	= "1.0";
		settings["classpath"] 		= "\"" + config.JarPath + ";" + String.Join(";", config.Libraries) + "\"";
		
		launchApp = jvmDir + "bin\\java.exe";
		launchArguments = "-Xmx" + memory + "m -Xms256m " + String.Join(" ", config.JVMArguments) + " " + config.MainClass + " " + String.Join(" ", config.GameArguments);
		foreach(KeyValuePair<string, string> entry in settings)
			if(launchArguments.Contains("${" + entry.Key + "}"))					
				launchArguments = launchArguments.Replace("${" + entry.Key + "}", entry.Value);
	}

	private void UnzipNatives(){
		foreach(string jar in config.Natives){
			string folder = settings["natives_directory"].Replace("\"", "");

			if(!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
			using (var unzip = new Unzip(jar))
{

				unzip.ExtractToDirectory(folder);
			}
		}
	}

	public void Start(){
		Console.WriteLine(launchArguments);

		UnzipNatives();
		Process p = new Process();
		p.StartInfo.FileName = launchApp;
		p.StartInfo.Arguments = launchArguments;
		p.StartInfo.WorkingDirectory = settings["game_directory"];
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.UseShellExecute = false;
		p.Start();
		while(!p.StandardOutput.EndOfStream)
			Console.WriteLine(p.StandardOutput.ReadLine());
	}
}

public class LaunchConfig {
	private JsonObject configFile;
	private LaunchConfig parentConfig;
	private string gameDir, versionDir, nativesDir, librariesDir, versionName;

	public string JarPath;

	public string AssetIndex {
		get { 
			if(configFile.Contains("assetIndex") && configFile["assetIndex"].Contains("id"))
				return configFile["assetIndex"].GetString("id");
			else if(parentConfig != null)
				return parentConfig.AssetIndex;
			return null;
		}
	}

	public string Type {
		get { return configFile.GetString("type") ?? parentConfig.Type; }
	}

	public string MainClass {
		get { return configFile.GetString("mainClass") ?? parentConfig.MainClass; }
	}

	public string[] Libraries {
		get { 
			List<string> librariesList = new List<string>();
			foreach(JsonObject obj in configFile.GetJsonArray("libraries").elements){
				if(CheckRules(obj.GetJsonArray("rules"))){
					JsonObject downloads = obj["downloads"] ?? obj;
					JsonObject artifact = downloads["artifact"] ?? downloads;
					
					if(artifact.Contains("path"))
						librariesList.Add(librariesDir + artifact.GetString("path").Replace("/", "\\"));
					else if(artifact.Contains("name")){
						string path = librariesDir + 
							artifact.GetString("name").Split(new char[]{':'})[0].Replace(".", "\\") + "\\" + 
							artifact.GetString("name").Split(new char[]{':'})[1] + "\\" + 
							artifact.GetString("name").Split(new char[]{':'})[2];
						foreach(string file in Directory.GetFiles(path))
							if(file.EndsWith(".jar"))
								librariesList.Add(file);
					}
				}
			}
			if(parentConfig != null)
				librariesList.AddRange(parentConfig.Libraries);
			return librariesList.ToArray();
		}
	}

	public string[] Natives {
		get { 
			List<string> nativesList = new List<string>();
			foreach(JsonObject obj in configFile.GetJsonArray("libraries").elements){
				if(CheckRules(obj.GetJsonArray("rules"))){
					JsonObject downloads = obj["downloads"] ?? obj;
					
					if(downloads.Contains("classifiers")){
						JsonObject classifiers = downloads["classifiers"];
						JsonObject natives = classifiers["windows"] ?? classifiers["natives-windows"];
						if(natives != null)
							nativesList.Add(librariesDir + natives.GetString("path").Replace("/", "\\"));
					}
				}
			}
			if(parentConfig != null)
				nativesList.AddRange(parentConfig.Natives);
			return nativesList.ToArray();
		}
	}

	public string[] GameArguments {
		get { 
			if(configFile.Contains("arguments")){
				List<string> argumentsList = GetArguments(configFile["arguments"].GetJsonArray("game"));
				if(parentConfig != null)
					argumentsList.AddRange(parentConfig.GameArguments);
				return argumentsList.ToArray();
			}else 
				return configFile.GetString("minecraftArguments").Split(new char[]{' '});
		}
	}

	public string[] JVMArguments {
		get { 
			if(configFile.Contains("arguments")){
				List<string> argumentsList = GetArguments(configFile["arguments"].GetJsonArray("jvm"));
				if(parentConfig != null)
					argumentsList.AddRange(parentConfig.JVMArguments);
				return argumentsList.ToArray();
			}
			else return new String[]{ "-cp", "${classpath}", "-Djava.library.path=${natives_directory}" };
		}
	}

	public LaunchConfig(string gameDir, string versionsDir, string versionName) {
		this.gameDir = gameDir;
		this.versionDir = versionsDir + "\\" + versionName + "\\";
		this.nativesDir = versionDir + "natives\\";
		this.librariesDir = gameDir + "libraries\\";
		this.versionName = versionName;
		this.JarPath = versionDir + "\\" + versionName + ".jar";
		configFile = new JsonObject(File.ReadAllText(versionDir + "\\" + versionName + ".json"));
		
		if(configFile.Contains("inheritsFrom"))
			parentConfig = new LaunchConfig(gameDir, versionsDir, configFile.GetString("inheritsFrom"));
	}

	private List<string> GetArguments(JsonArray arguments){
		List<string> argumentsList = new List<string>();
		foreach(object obj in arguments.elements){
			if(obj is JsonObject){
				JsonObject jsonObj = obj as JsonObject;
				if(CheckRules(jsonObj.GetJsonArray("rules"))){
					object value = jsonObj.Get("values") ?? jsonObj.Get("value");
					if(value is JsonArray) {
						foreach(object argument in (value as JsonArray).elements)
							argumentsList.Add(argument.ToString().Replace(" ", ""));
					}else argumentsList.Add(value.ToString().Replace(" ", ""));
				}
			}else argumentsList.Add(obj.ToString().Replace(" ", ""));
		}
		return argumentsList;
	}

	private bool CheckRules(JsonArray rules){
		if(rules == null)
			return true;
		Dictionary<string, object> features = new Dictionary<string, object>();
		features["is_demo_user"] 		= false;
		features["has_custom_resolution"] 	= false;
		Dictionary<string, object> os = new Dictionary<string, object>();
		os["name"] 				= "windows";
		os["version"] 				= "7";
		os["arch"] 				= "x64";
		for(int i = 0; i < rules.elements.Count; i++){
			JsonObject rule = rules.GetJsonObject(i);
			if(rule.GetString("action") != "allow")
				continue;
		
			if(rule.Contains("features"))
				foreach(KeyValuePair<string, object> feature in rule["features"].elements)
					if(features.ContainsKey(feature.Key) && features[feature.Key].ToString() == feature.Value.ToString())
						return false;
					
			if(rule.Contains("os"))
				foreach(KeyValuePair<string, object> feature in rule["os"].elements)
					if(os.ContainsKey(feature.Key) && os[feature.Key].ToString() != feature.Value.ToString())
						return false;
		}
		return true;
	}
}



	

