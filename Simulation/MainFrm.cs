using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using NumSimLib;
using NumSimLib.Solver;
using GraphPlot;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.DataVisualization.Charting;
using GenericSettingsForm;
using System.IO;
using ThreadSafeExtensions;
using System.Runtime.InteropServices;

namespace SimulationGUI {
	public partial class MainFrm : Form {
		SimConfig cfg;
		SimConfig file_cfg;

		Timer _tmrGUI;
		int nGraphs = 2;
		Graph[] gr;

		SimulationCtrl simCtrl = new SimulationCtrl();
		Simulation _simLast = null;

		string sCurFileName = "";
		double _fRunTimeMS;

		#region constructor
		public MainFrm() {
			InitializeComponent();


			gr = new Graph[nGraphs];
			for(int i = 0; i < nGraphs; i++) {
				gr[i] = new Graph();
				Controls.Add(gr[i]);
			}

			_tmrGUI = new Timer();
			_tmrGUI.Interval = 250;
			_tmrGUI.Tick += _tmrGUI_Tick;
			_tmrGUI.Start();

			//start with empty simulation
			MakeNew();


			simCtrl.OnSimulationComplete += sim_OnSimulationComplete;
			simCtrl.OnSimulationFailed += simCtrl_OnSimulationFailed;


			
			simCtrl.Start();
		}

		void _tmrGUI_Tick(object sender, EventArgs e) {
			if(cfg != null && file_cfg != null) {
				this.Text = "Simulation: " + cfg.Name + (cfg.Compare(file_cfg) ? "" : "*");
			}

			for(int i = 0; i < gr.Length; i++)
				gr[i].Visible = true;
			if(simCtrl.GetSolverProgress() == 100)
				lblProgress.Text = "Done(" + (_fRunTimeMS * 0.001).ToString("0.0000") + " [s])";
			else
				lblProgress.Text = "Progress: " + simCtrl.GetSolverProgress().ToString() + @" %";

		}
		#endregion

		#region simulation handling
		private void MakeNew() {
			sCurFileName = "";

			cfg = new SimConfig();
			cfg.sPlotSettings = new string[gr.Length][];
			for(int i = 0; i < gr.Length; i++) {
				cfg.sPlotSettings[i] = gr[i].GetConfig();
			}

			cfg.Name = "Default";
			string sDefSet = @"Line,True,False,Series1,Red,White,Black,False,-100,100,-100,100,10,10,2,Dot,LightGray,None,Red,Numeric,True,5,Consolas; 12pt,X - axis,Y - axis,False,False,ArrayVsIndex,50,1000,Trace,,0.00,False,False,0,Microsoft Sans Serif; 8.25pt";
			cfg.sPlotSettings = new string[gr.Length][];
			for(int i = 0; i < gr.Length; i++)
				cfg.sPlotSettings[i] = sDefSet.Split(',');

			file_cfg = cfg.Clone();
			UpdateGUI();
		}

		private void SaveFile(string sFileName) {
			sCurFileName = sFileName;
			SimConfig.SaveConfig(sFileName, cfg);
			file_cfg = cfg.Clone();
		}

		private void LoadFile(string sFileName) {
			sCurFileName = sFileName;
			SimConfig.LoadConfig(sFileName, out cfg);
			//SimConfig.LoadConfig(sFileName, out file_cfg);
			file_cfg = cfg.Clone();

			//create default string, can be copied from watch and hardcoded
			//string sTmp = String.Join(",",cfg.sPlotSettings[0]);
			
			
			UpdateGUI();
		}
		private void DoSimulation() {
			if(cfg != null && cfg.Exps != null) {

				//can have multiple combinations of model and solver, but always same conditions, different experiments, same simulation. For comparison.
				//if need separate N,dt, initial conditions, etc, use two different simulations
				for(int nSim = 0; nSim < cfg.Exps.Length; nSim++) {
					//create model and solver
					ODEModel model = Factory.MakeModel(cfg.Exps[nSim].Model);
					ODESolver solver = Factory.MakeSolver(cfg.Exps[nSim].Solver);
					//cfg.Simulations[nSim].Input;

					//create simulation and setup
					Simulation sim = new Simulation();
					if(cfg.InputFromFile) {
						string[] u;
						if(File.Exists(cfg.InputFileName)){
							List<string> lst = File.ReadAllLines(cfg.InputFileName).ToList<string>();
							lst.RemoveAll((x)=>{return (x == "" || x[0] == '%');});
							u = lst.ToArray();
							sim.Setup(nSim, solver, model, cfg.x0, cfg.N, cfg.dt, u, cfg.ParamFileName);
						}
						else
							MessageBox.Show("Input file not found");
						
					}
					else
						sim.Setup(nSim, solver, model, cfg.x0, cfg.N, cfg.dt, cfg.u, cfg.ParamFileName);

					//que simulation
					simCtrl.Que(sim);
				}
			}
		}

		private void ClearSimulation() {			
			for(int i = 0; i < gr.Length; i++) {
				for(int k = 0; k < 8; k++)
					gr[i].SetY(k, new double[0]);
			}
		}

		void simCtrl_OnSimulationFailed(string sError) {
			MessageBox.Show(sError, "Simulation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		void sim_OnSimulationComplete(Simulation sim, double fRunTimeMS) {
			ThreadSafe.Do(this, () => {
				lblProgress.Text = "Plotting results";
			});

			//save last simulation
			_simLast = sim;
			_fRunTimeMS = fRunTimeMS;

			double[] x = new double[sim.N / cfg.PlotStep];
			for(int i = 0; i < sim.N / cfg.PlotStep; i++)
				x[i] = (double)i * cfg.PlotTimeStep;

			for(int i = 0; i < cfg.PlotMap.Length; i++) {

				int nGraph=0,nChannel=0,nSim = -1, nValIndex = 0;				
				string[] sTmp;
				bool b = true;

				sTmp = cfg.PlotMap[i][0].Split('.');
				if(sTmp.Length == 2) {
					b &= int.TryParse(sTmp[0], out nGraph);
					b &= int.TryParse(sTmp[1], out nChannel);
				}
				else
					b = false;

				sTmp = cfg.PlotMap[i][1].Split('.');
				if(sTmp.Length == 2) {
					b &= int.TryParse(sTmp[0], out nSim);
					b &= int.TryParse(sTmp[1].Substring(1), out nValIndex);
				}
				else
					b = false;

				if(b) {
					if(nSim == sim.Index) {
						switch(sTmp[1][0]) {
							case 'x': gr[nGraph].SetXY(nChannel, x, sim.GetState(nValIndex, cfg.PlotStep, -1000, 1000)); break;
							case 'u': gr[nGraph].SetXY(nChannel, x, sim.GetInput(nValIndex, cfg.PlotStep, -1000, 1000)); break;
							case 'y': gr[nGraph].SetXY(nChannel, x, sim.GetMeasurment(nValIndex, cfg.PlotStep, -1000, 1000)); break;
						}
					}
				}
			}
		}
		#endregion

		void UpdateGUI() {
			if(cfg != null) {
				if(gr != null && cfg.sPlotSettings != null && cfg.sPlotSettings.Length != 0) {
					double fTotalHeights = 0;
					for(int i = 0; i < cfg.PlotHeights.Length; i++)
						fTotalHeights += cfg.PlotHeights[i];

					int nAccGrPos = 20;
					for(int i = 0; i < gr.Length; i++) {
						double fGraphHeight = ((double)Height - 50) * cfg.PlotHeights[i % cfg.PlotHeights.Length] / fTotalHeights;
						gr[i].Location = new Point(0, nAccGrPos);
						gr[i].Size = new Size(Width, (int)fGraphHeight);
						nAccGrPos += (int)fGraphHeight;

						gr[i].Stop();
						gr[i].SetConfig(cfg.sPlotSettings[i % cfg.sPlotSettings.Length]);
						gr[i].Setup();
						gr[i].Start();
					}
				}
			}
		}
		#region GUI event handlers		
		void runToolStripMenuItem_Click(object sender, EventArgs e) { DoSimulation(); }

		private System.Windows.Forms.DialogResult CheckChangePopSave() {
			if(!cfg.Compare(file_cfg)) {
				System.Windows.Forms.DialogResult res = MessageBox.Show("Configuration of current simulation changes. Save file before continuing?", "Configuration changed", MessageBoxButtons.YesNoCancel);
				if(res == System.Windows.Forms.DialogResult.Yes) {
					saveToolStripMenuItem_Click(null, null);
					
				}
				return res;
			}
			return System.Windows.Forms.DialogResult.No;
		}
		protected override void OnClosing(CancelEventArgs e) {
			base.OnClosing(e);
			if(CheckChangePopSave() == System.Windows.Forms.DialogResult.Cancel)
				e.Cancel = true;
		}
		protected override void OnClosed(EventArgs e) {
			base.OnClosed(e);
			simCtrl.Stop();
		}

		private void ShowGraphSettings(int index) {
			gr[index].ShowSettings(false);			
			gr[index].Stop();
			gr[index].Setup();
			gr[index].Start();
			cfg.sPlotSettings[index] = gr[index].GetConfig();
		}


		//show simulation settings for current file/definition
		private void simulationToolStripMenuItem1_Click(object sender, EventArgs e) {
			if(cfg != null) {


				ModelType[] model = new ModelType[cfg.Exps.Length];
				SolverType[] solver = new SolverType[cfg.Exps.Length];

				for(int i = 0; i < cfg.Exps.Length; i++) {
					model[i] = cfg.Exps[i].Model;
					solver[i] = cfg.Exps[i].Solver;
				}

				SettingsForm dlg = new SettingsForm("Simulation settings", false);
				dlg.AddProperty("Name", cfg.Name, false, true);
				dlg.AddProperty("N", cfg.N, false, true);
				dlg.AddProperty("dt", cfg.dt, false, true);
				dlg.AddArrayProperty("Initial conditions", cfg.x0, false, true, true, 0, 0);
				dlg.AddFileProperty("Parameter file name", cfg.ParamFileName, false, true);


				dlg.AddProperty("Numbe of experiments", cfg.Exps.Length, false, true);
				dlg.AddEnumArrayProperty<ModelType>("Model", model, false, true, false, 0, 0);
				dlg.AddEnumArrayProperty<SolverType>("Solver", solver, false, true, false, 0, 0);

				int nLabel = Math.Max(cfg.PlotMap.Length, 1);
				string[][] sLabels = new string[nLabel][];
				for(int i = 0; i < nLabel; i++)
					sLabels[i] = new string[3] { "Define data for plot " + (i + 1).ToString(), "Gr.Ch", "Sim.Pr" };

				dlg.AddValuePairArrayProperty("Plot map", cfg.PlotMap, false, true, true, 0, "", sLabels);

				dlg.AddProperty("Get input from file", cfg.InputFromFile, false, true);
				dlg.AddArrayProperty("Input", cfg.u, false, true, true, 0, "");
				dlg.AddFileProperty("Input file name", cfg.InputFileName, false, true); 
				dlg.AddArrayProperty("Plot heights (relative)", cfg.PlotHeights, false, true, false, 0, 0);
				dlg.AddProperty("Plot steps prescaling", cfg.PlotStep, false, true);
				dlg.AddProperty("Plot time step", cfg.PlotTimeStep, false, true);
				
				

				dlg.SetChangeAction(5, dlg, (x) => {
					int n = (int)dlg.GetValue(5);
					dlg.SetArraySize(6, n);
					dlg.SetArraySize(7, n);					
					dlg.UpdatePanels();
				});
				dlg.SetChangeAction(9, dlg, (x) => {
					bool b = (bool)dlg.GetValue(9);
					dlg.ShowPanel(11, b);
					dlg.ShowPanel(10, !b);					
					dlg.UpdatePanels();
				});

				dlg.ShowDialog();
				cfg.Name = (string)dlg.GetValue(0);
				cfg.N = (int)dlg.GetValue(1);
				cfg.dt = (double)dlg.GetValue(2);
				cfg.x0 = (double[])dlg.GetValue(3);
				cfg.ParamFileName = (string)dlg.GetValue(4);

				int nLen = (int)dlg.GetValue(5);
				string[] sModel = (string[])dlg.GetValue(6);
				string[] sSolver = (string[])dlg.GetValue(7);
				cfg.PlotMap = (string[][])dlg.GetValue(8);

				
				cfg.InputFromFile = (bool)dlg.GetValue(9);
				cfg.u = (string[])dlg.GetValue(10);				
				cfg.InputFileName = (string)dlg.GetValue(11);
				cfg.PlotHeights = (double[])dlg.GetValue(12);
				cfg.PlotStep = (int)dlg.GetValue(13);
				cfg.PlotTimeStep = (double)dlg.GetValue(14);



				cfg.Exps = new ExpConfig[nLen];
				for(int i = 0; i < nLen; i++) {
					cfg.Exps[i] = new ExpConfig();
					Enum.TryParse<ModelType>(sModel[i], out cfg.Exps[i].Model);
					Enum.TryParse<SolverType>(sSolver[i], out cfg.Exps[i].Solver);
				}
				//ClearSimulation();
				UpdateGUI();
			}
		}

		
		protected override void OnSizeChanged(EventArgs e) {
			base.OnSizeChanged(e);
			UpdateGUI();
			
		}
		#endregion

		#region file handling
		private void newToolStripMenuItem_Click(object sender, EventArgs e) {

			if(CheckChangePopSave() != System.Windows.Forms.DialogResult.Cancel)
				MakeNew();
			
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e) {
			if(CheckChangePopSave() != System.Windows.Forms.DialogResult.Cancel) {
				OpenFileDialog dlg = new OpenFileDialog();
				dlg.Filter = "Simulation file (*.sim)|*.sim";
				if(dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					LoadFile(dlg.FileName);
				}
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			if(sCurFileName == "")
				saveAsToolStripMenuItem_Click(sender, e);
			else
				SaveFile(sCurFileName);
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.Filter = "Simulation file (*.sim)|*.sim";
			if(dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				SaveFile(dlg.FileName);
			}
		}
		#endregion

		private void plot1ToolStripMenuItem_Click(object sender, EventArgs e) { ShowGraphSettings(0); }

		private void plot2ToolStripMenuItem_Click(object sender, EventArgs e) { ShowGraphSettings(1); }

		[DllImport("user32.dll")]
		public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
		public void ScreenShot(string sFileName, ImageFormat format) {
						

			IntPtr hwnd = Handle; ;

			Bitmap bmp = new Bitmap(Size.Width, Size.Height);
			Graphics gfxBmp = Graphics.FromImage(bmp);
			IntPtr hdcBitmap = gfxBmp.GetHdc();
			PrintWindow(hwnd, hdcBitmap, 0);
			gfxBmp.ReleaseHdc(hdcBitmap);
			gfxBmp.Dispose();


			Bitmap bmp2 = new Bitmap(Width, Height);
			Graphics gr = Graphics.FromImage(bmp2);
			Rectangle rSrc = new Rectangle(0, 0, Width, Height);
			Rectangle rDst = new Rectangle(0, 0, Width, Height);
			gr.DrawImage(bmp, rDst, rSrc, GraphicsUnit.Pixel);


			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
			ImageCodecInfo cdcOutput = null;
			foreach(ImageCodecInfo codec in codecs) {
				if(codec.FormatID == format.Guid) {
					cdcOutput = codec;
				}
			}

			if(cdcOutput == null)
				bmp2.Save(sFileName, format);
			else {
				System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
				EncoderParameters myEncoderParameters = new EncoderParameters(1);
				EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 100L);
				myEncoderParameters.Param[0] = myEncoderParameter;

				bmp2.Save(sFileName, cdcOutput, myEncoderParameters);
			}



		}
		private void screenshotToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.Filter = "JPG (*.jpg)|*.jpg|BMP (*.bmp)|*.bmp";
			if(dlg.ShowDialog() == DialogResult.OK) {
				switch(dlg.FilterIndex) {
					case 1:
						ScreenShot(dlg.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
						break;
					case 2:
						ScreenShot(dlg.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
						break;
				}

			}
		}
		protected int GetElements(double [][] a){
			if(a == null || a.Length == 0)
				return 0;
			else if(a[0] == null || a[0].Length == 0)
				return 0;
			else
				return a[0].Length;
		}
		private void SaveFile(string sFileName, string sSep, bool bHeader=true) {
			int N = _simLast.N;
			
			List<string> lstLines = new List<string>();

			if(bHeader) {
				string sModelType = _simLast.Model.GetType().ToString().Replace("SimulationGUI.Model","");
				switch(sModelType){
					case "R4C2":
						lstLines.Add(String.Join(sSep, new string[] { "Qheater", "Qpeople", "Qappliences", "Qsolar", "Qextsolar", "T_inf", "V_e", "T_b", "T_w" }));
						break;
					case "R6C2":
						lstLines.Add(String.Join(sSep, new string[] { "Qheater", "Qpeople", "Qappliences", "Qsolar", "Qextsolar", "T_inf", "V_e", "T_b", "T_w", "T_s", "T_h" }));
						break;
					case "R6C3":
						lstLines.Add(String.Join(sSep, new string[] { "Qheater", "Qpeople", "Qappliences", "Qsolar", "Qextsolar", "T_inf", "V_e", "T_b", "T_w", "T_s", "T_h" }));
						break;
					case "R7C3":
						lstLines.Add(String.Join(sSep, new string[] { "Qheater", "Qpeople", "Qappliences", "Qsolar", "Qextsolar", "T_inf", "V_e", "T_b", "T_w1", "T_w2", "T_s", "T_h" }));
						break;
					case "SingleZone":
						lstLines.Add(String.Join(sSep, new string[] { 
							"Qheater", "Qpeople", "Qappliences", "Qsolar", "Qfloor1", "Qfloor2", "Qfloor3", "T_inf", "N", "RH",
							"Rho_b", "T_b", "T_1w", "T_2w", "T_3w", "T_4w", "T_1f", "T_2f", "T_3f", "T_1r", "T_2r", "T_3r", "T_4r", "T_1fur", "T_2fur", "T_3fur", "T_4fur", "T_5fur" }));
						break;
				}
			}



			int nu = GetElements(_simLast.fInput);
			int nx = GetElements(_simLast.Result); 
			int ny = GetElements(_simLast.Measurments);
			string[] sVals = new string[nu + nx + ny];

			for(int i = 0; i < N; i+=cfg.PlotStep) {
				for(int k = 0; k < nu; k++)
					sVals[k] = _simLast.fInput[i][k].ToString();

				for(int k = 0; k < nx; k++)
					sVals[nu + k] = _simLast.Result[i][k].ToString();

				for(int k = 0; k < ny; k++)
					sVals[nu + nx + k] = _simLast.Measurments[i][k].ToString();
				lstLines.Add(String.Join(sSep, sVals));
			}
			File.WriteAllLines(sFileName, lstLines.ToArray());
		}
		private void exportCSVToolStripMenuItem_Click(object sender, EventArgs e) {
			if(_simLast != null) {
				SaveFileDialog dlg = new SaveFileDialog();
				dlg.Filter = "CSV (*.csv)|*.csv|TSV (*.tsv)|*.tsv";
				if(dlg.ShowDialog() == DialogResult.OK) {
					switch(dlg.FilterIndex) {
						case 1:
							SaveFile(dlg.FileName, ",");
							break;
						case 2:
							SaveFile(dlg.FileName,"\t");
							break;
					}
				}
			}
		}
	}
}
