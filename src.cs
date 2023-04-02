using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

internal static class NativeMethods {
	[DllImport("kernel32.dll")]
	internal static extern Boolean AllocConsole();
}

public class Program: Form {

	[STAThread]
	static void Main(string[] args){
		NativeMethods.AllocConsole();
		Application.EnableVisualStyles();
		Application.Run(new Program());
	}

	private Button launchBtn;
	private ComboBox version;
	private TrackBar memory;

	private bool isJavaReady = false;
	private bool isMinecraftReady = false;
	
	public Program(){
		Console.WriteLine("-------------------------------------");
		Console.WriteLine("Offline Minecraft launcher by Husker");
		Console.WriteLine("-------------------------------------");

		this.Width = 500;
		this.Height = 270;
		this.Text = "Launcher";
		this.FormBorderStyle = FormBorderStyle.FixedSingle;
		this.MaximizeBox = false;
		this.CenterToScreen();
		
		TextBox nickname = AddTextBoxLine(0, 20, 100, "Ник:");
		nickname.Text = "Player";

		TextBox javaPath = AddPathLine(0, 50, "Путь к Java:");
		TextBox minecraftPath = AddPathLine(0, 80, "Путь к Minecraft:");

		version = AddComboBoxLine(0, 110, 150, "Версия:");
		version.Enabled = false;
		version.DropDownStyle = ComboBoxStyle.DropDownList;

		memory = AddTrackBarLine(0, 140, 150, "Память:", 256, "Мб");
		memory.Enabled = false;
		memory.Minimum = 1;
		memory.Maximum = 4096 / 256;
		memory.Value = 1024 / 256;
		memory.TickFrequency = 1;

		launchBtn = new Button() {
			Text = "Запустить",
			Location = new Point(150, 190),
			Size = new Size(200, 40),
			Enabled = false
		};
		launchBtn.Click += new EventHandler((sender, e) => {
			Hide();
			try{
				new VersionLauncher(
					javaPath.Text + "\\",
					minecraftPath.Text + "\\",
					version.SelectedItem.ToString(),
					nickname.Text,
					memory.Value * 256
				).Start();

			}catch(Exception a){
				Console.WriteLine(a.ToString());
			}
			Show();
		});
		this.Controls.Add(launchBtn);

		minecraftPath.TextChanged += new EventHandler((sender, e) => {
			try{
				version.Items.Clear();
				foreach(string v in ClientInfo.GetVersions(minecraftPath.Text))
					version.Items.Add(v);
				version.SelectedIndex = 0;

				isMinecraftReady = true;
			}catch {
				isMinecraftReady = false;
			}
			CheckReady();
		});
		javaPath.TextChanged += new EventHandler((sender, e) => {
			try{
				Process p = new Process();
				p.StartInfo.FileName = javaPath.Text + "\\bin\\java.exe";
				p.StartInfo.Arguments = "-version";
				p.StartInfo.UseShellExecute = false;
				p.Start();
				isJavaReady = true;
			}catch {
				isJavaReady = false;
			}
			CheckReady();
		});
	}

	public void CheckReady(){
		launchBtn.Enabled = isMinecraftReady && isJavaReady;
		version.Enabled = isMinecraftReady;
		memory.Enabled = isMinecraftReady;
	}

	public Control AddControlLine(int x, int y, string text, Control control){
		Label lbl = new Label();
		lbl.TextAlign = ContentAlignment.MiddleRight;
		lbl.Location = new Point(x, y);
		lbl.Size = new Size(100, 20);
		lbl.Text = text;
		this.Controls.Add(lbl);
		
		control.Location = new Point(x + 100, y);
		this.Controls.Add(control);
		return control;
	}


	public ComboBox AddComboBoxLine(int x, int y, int width, string text){
		ComboBox cb = new ComboBox();
		cb.Size = new Size(width, 30);
		
		return (ComboBox)AddControlLine(x, y, text, cb);
	}

	public TrackBar AddTrackBarLine(int x, int y, int width, string text, int multiplier, string postfix){
		Label lbl = new Label();
		lbl.Location = new Point(x + width + 100, y);
		lbl.Size = new Size(100, 20);
		lbl.TextAlign = ContentAlignment.MiddleLeft;
		this.Controls.Add(lbl);

		TrackBar tb = new TrackBar();
		tb.Size = new Size(width, 30);
		tb.ValueChanged += new EventHandler((sender, e) => {
			lbl.Text = tb.Value * multiplier + " " + postfix;
		});
		return (TrackBar)AddControlLine(x, y, text, tb);
	}

	public TextBox AddTextBoxLine(int x, int y, int width, string text){
		TextBox tb = new TextBox();
		tb.Size = new Size(width, 30);
		
		return (TextBox)AddControlLine(x, y, text, tb);
	}

	public TextBox AddPathLine(int x, int y, string text){
		TextBox tb = AddTextBoxLine(x, y, 300, text);
		Button btn = new Button();
		btn.Location = new Point(x + 405, y - 1);
		btn.Text = "Обзор...";
	
		btn.Click += new EventHandler((sender, a) => {
			FolderBrowserDialog opnDlg = new FolderBrowserDialog();
			if(opnDlg.ShowDialog() == DialogResult.OK)
				tb.Text = opnDlg.SelectedPath;
		});
		this.Controls.Add(btn);
		return tb;
	}
}