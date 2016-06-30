using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NumSimLib;
using NumSimLib.Solver;

namespace SimulationGUI {
	public enum ModelType { LP, SingleZone, R4C2, R6C2, R6C3, R7C3, R5C3, R4C2_ONOFF };
	public class ExpConfig {
		public ModelType Model = ModelType.LP;
		public SolverType Solver = SolverType.FE;

		public override bool Equals(object obj) {
			ExpConfig cfg = (ExpConfig)obj;
			return (cfg.Model == Model && cfg.Solver == Solver);
		}
		public ExpConfig Clone() {
			return new ExpConfig() { Model = Model, Solver = Solver};
		}
	}

	public class SimConfig {
		public string[][] sPlotSettings;
		public string Name = "default";
		public int N = 0;
		public double dt = 1;
		public double[] x0 = new double[0];
		public ExpConfig[] Exps = new ExpConfig[0];
		public string[][] PlotMap = new string[0][] { };
		public string[] u = new string[1] { "0" };
		public string ParamFileName = "";
		public double[] PlotHeights = new double[2] { 50, 50 };		
		public bool InputFromFile = false;
		public string InputFileName = "";

		public int PlotStep = 1;
		public double PlotTimeStep = 1;

		public static void SaveConfig(string sFileName, SimConfig cfg) {
			try {
				XmlSerializer writer = new XmlSerializer(typeof(SimConfig));
				StreamWriter file = new StreamWriter(sFileName);
				writer.Serialize(file, cfg);
				file.Close();
			}
			catch(Exception ex) {
				string s = ex.Message;				
			}
		}
		public static bool LoadConfig(string sFileName, out SimConfig cfg) {
			cfg = null;
			try {
				XmlSerializer reader = new XmlSerializer(typeof(SimConfig));
				StreamReader file = new StreamReader(sFileName);
				cfg = (SimConfig)reader.Deserialize(file);
				file.Close();
				return true;
			}
			catch(Exception ex) {
				string s = ex.Message;				
				return false;
			}
		}
		
		private bool CompareArray<T>(T[] a1, T[] a2){			
			if(a1 == null && a2 == null)
				return true;
			else if((a1 == null && a2 != null) || (a1 != null && a2 == null))
				return false;
			else if(a1.Length != a2.Length)
				return false;
			else {
				for(int i = 0; i < a1.Length; i++) 
					if(!a1[i].Equals(a2[i]))
						return false;				
			}
			return true;
		}
		private bool Compare2DStringArray(string[][] a1, string[][] a2) {
			if(a1 == null && a2 == null)
				return true;
			else if((a1 == null && a2 != null) || (a1 != null && a2 == null))
				return false;
			else if(a1.Length != a2.Length)
				return false;
			else {
				for(int i = 0; i < a1.Length; i++)
					if(!CompareArray<string>(a1[i],a2[i]))
						return false;
			}
			return true;
		}
		private T[] CopyArray<T>(T[] a){
			T[] ret;
			if(a == null)
				return null;
			else{
				ret = new T[a.Length];
				for(int i = 0; i < a.Length; i++)
					ret[i] = a[i];						
			}
			return ret;
		}
		private string[][] Copy2DStringArray(string[][] a) {
			string[][] ret;
			if(a == null)
				return null;
			else {
				ret = new string[a.Length][];
				for(int i = 0; i < a.Length; i++)
					ret[i] = CopyArray<string>(a[i]);
			}
			return ret;
		}
		public bool Compare(SimConfig cfg){
			bool bEqual = true;
			
			bEqual &= Compare2DStringArray(sPlotSettings, cfg.sPlotSettings);			
			bEqual &= (Name == cfg.Name);
			bEqual &= (N == cfg.N);
			bEqual &= (dt == cfg.dt);
			bEqual &= CompareArray<double>(x0, cfg.x0);
			bEqual &= CompareArray<ExpConfig>(Exps, cfg.Exps);
			bEqual &= Compare2DStringArray(PlotMap, cfg.PlotMap);
			bEqual &= CompareArray<string>(u, cfg.u);		
			bEqual &= (ParamFileName == cfg.ParamFileName);
			bEqual &= CompareArray<double>(PlotHeights, cfg.PlotHeights);
			bEqual &= (PlotStep == cfg.PlotStep);
			bEqual &= (PlotTimeStep == cfg.PlotTimeStep);			
			bEqual &= (InputFromFile == cfg.InputFromFile);
			bEqual &= (InputFileName == cfg.InputFileName);			
			return bEqual;
		}

		public  SimConfig Clone() {
			SimConfig cfg = new SimConfig();
			cfg.sPlotSettings = Copy2DStringArray(sPlotSettings);
			cfg.Name = Name;
			cfg.N = N;
			cfg.dt = dt;
			cfg.x0 = CopyArray<double>(x0);
			if(Exps == null)
				cfg.Exps = null;
			else {
				cfg.Exps = new ExpConfig[Exps.Length];
				for(int i = 0; i < Exps.Length; i++)
					cfg.Exps[i] = Exps[i].Clone();
			}

			cfg.PlotMap = Copy2DStringArray(PlotMap);
			cfg.u = CopyArray<string>(u);
			cfg.ParamFileName = ParamFileName;
			cfg.PlotHeights = CopyArray<double>(PlotHeights);
			cfg.PlotStep = PlotStep;
			cfg.PlotTimeStep = PlotTimeStep;
			cfg.InputFromFile = InputFromFile;
			cfg.InputFileName = InputFileName;
			
			return cfg;
		}
	}
}
