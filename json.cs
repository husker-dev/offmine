using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections;

public class Json {

	protected object nextElement(string source, ref int i){
		String varValue = "";
		int type = 0, insideBlocksCount = 0;
		bool isInsideBrakets = false;

		for(;i < source.Length;i++){
			char ch = source[i];

			if(type == 0){
				if(ch == '[')
					type = 1;
				else if(ch == '{')
					type = 2;
				else if(ch == '\"'){
					type = 3;
					continue;
				}
				else if(Char.IsNumber(ch))
					type = 4;
				else if(ch == 't') {
					i += 4;
					return false;
				}
				else if(ch == 'f') {
					i += 5;
					return false;
				}
				else continue;
			}

			if((type == 3 && ch == '\"' && source[i-1] != '\\') || 
			   (type == 4 && !Char.IsNumber(source[i])))
				return varValue;

			varValue += ch;

			if(type == 1 || type == 2){
				if(ch == '\"' && source[i-1] != '\\')
					isInsideBrakets = !isInsideBrakets;

				if(!isInsideBrakets){
					if(type == 1){
						if(ch == '[') 
							insideBlocksCount ++;
						if(ch == ']' && --insideBlocksCount == 0)
							return new JsonArray(varValue);	
					}
					if(type == 2){
						if(ch == '{') 
							insideBlocksCount ++;
						if(ch == '}' && --insideBlocksCount == 0)
							return new JsonObject(varValue);
					}
				}
			}
		}
		return null;
	}
}

public class JsonObject: Json {
	public string source;
	public Dictionary<string, object> elements = new Dictionary<string, object>();

	public JsonObject(string source){
		this.source = source.Trim();
		
		for(int i = 1; i < this.source.Length; i++){
			if(this.source[i] == '\"'){
				string varName = "";
				
				for(i++; this.source[i] != '\"';i++)
					varName += this.source[i];
				i++;
				elements[varName] = nextElement(this.source, ref i);
			}
		}		
	}

	public bool Contains(string key) {
		return elements.ContainsKey(key);
	}

	public JsonObject this[string key] {
		get { return (JsonObject)Get(key); }
	}

	public object Get(string key) {
		if(!elements.ContainsKey(key))
			return null;
			//throw new Exception("The given key '" + key + "' is not found.");
		return elements[key];
	}

	public string GetString(string key) {
		return (String)Get(key);
	}

	public JsonArray GetJsonArray(string key) {
		return (JsonArray)Get(key);
	}
}

public class JsonArray: Json {
	public string source;
	public List<object> elements = new List<object>();

	public JsonArray(string source){
		this.source = source.Trim();
		
		for(int i = 1; i < this.source.Length; i++){
			object element = nextElement(this.source, ref i);
			if(element != null)
				elements.Add(element);
		}
	}

	public object Get(int index) {
		return elements[index];
	}

	public string GetString(int index) {
		return (String)elements[index];
	}

	public JsonObject GetJsonObject(int index) {
		return (JsonObject)elements[index];
	}

	public JsonArray GetJsonArray(int index) {
		return (JsonArray)elements[index];
	}
}