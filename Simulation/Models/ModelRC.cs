using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumSimLib;

namespace SimulationGUI {
	class ModelRC:ODEModel {
		//Ventilation model
		//double R_vmin, R_vmax;
		double G_vent;

		public override void Setup() {
			//Ventilation
			//R_vmax = par("R_vmax");
			//R_vmin = par("R_vmin");
			double R_vent = par("R_vent");
			G_vent = 1 / R_vent;

			base.Setup();
		}
		/*
		protected double VentilationMinMax(double V_e) {
			//ventialtion equivalent resistance
			double R_v = double.MaxValue;

			if(V_e > 0) {
				double scale = 1.0 - (V_e / 1000);
				double G_max = 1 / R_vmax;
				double G_min = 1 / R_vmin;

				double G_v = (G_max - G_min) * scale + G_min;
				R_v = 1 / G_v;
			}
			return R_v;
		}
		*/
		protected double Ventilation(double N) {
			double R_v = double.MaxValue;
			if(N > 0) {
				R_v = 1 / (G_vent * N);
			}
			return R_v;
		}
	}
}
