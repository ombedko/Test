using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulationGUI {
	class ModelR6C2:ModelRC {
		//parameters
		double R_b, R_s, R_w, R_g, R_e;
		double C_b, C_w;


		//measurments
		double T_s, T_h;

		public override void Setup() {
			base.Setup();

			//PARAMETERS			

			//THERMAL RESISTANCES			
			R_b = par("R_b");
			R_s = par("R_s");
			R_w = par("R_w");

			//connecting thermal resistances for window, door, floor and roof in paralell
			R_g = par("R_g");

			//R_e = 0 ignores Q3
			R_e = par("R_e");



			//THERMAL CAPACITANCE
			C_b = par("C_b");
			C_w = par("C_w");			



		}
		public override void EndStep() {
			base.EndStep();
		}
		public override double[] Measurments(double[] x) {
			double[] y = new double[2];
			y[0] = T_s;
			y[1] = T_h;
			return y;
		}
		public override double[] dxdt(double[] x, double t, double[] u) {
			base.dxdt(x, t, u);

			#region extract states and inputs from arguments
			//number of states, alocate return array
			int nx = 2;
			if(x.Length != nx)
				throw new Exception("Not enough states in x0. Found " + x.Length.ToString() + ", need " + nx.ToString());
			int nu = 7;
			if(u.Length != nu)
				throw new Exception("Not enough inputs in u. Found " + u.Length.ToString() + ", need " + nu.ToString());

			//EXTRACT STATES
			double T_b = x[0];
			double T_w = x[1];

			//EXTRACT INPUTS

			//heat sources
			double Qheater = u[0];
			double Qpeople = u[1];
			double Qappliences = u[2];
			double Qsolar = u[3];
			double Qextsolar = u[4];
			//outside weath condition parameters
			double T_inf = u[5];

			//ventilation
			double V_e = u[6];
			#endregion

			#region Model

			//sum up heat sources
			double Q1 = Qheater + Qappliences;
			double Q2 = Qsolar;
			double Q3 = Qextsolar;
			
			//ventialtion equivalent resistance
			double R_v = Ventilation(V_e);
			

			//ALGEBRAIC NODE EQUATIONS (store for later return by Measurments)
			T_s = (R_b * R_s * Q2 + R_b * T_w + R_s * T_b) / (R_b + R_s);
			T_h = (R_e * R_w * Q3 + R_e * T_w + R_w * T_inf) / (R_e + R_w);

			//DIFFERENTIAL NODE EQUATIONS
			double dT_b = 1 / C_b * Q1    -    1 / (C_b * R_b) * (T_b - T_s)    -    1 / (C_b * R_g) * (T_b - T_inf)    -    1 / (C_b * R_v) * (T_b - T_inf);
			double dT_w =                 -    1 / (C_w * R_s) * (T_w - T_s)    -    1 / (C_w * R_w) * (T_w - T_h);

			#endregion

			#region return differentials
			//RETURN DIFFERENTIALS
			double[] dxdt = new double[nx];
			dxdt[0] = dT_b;

			dxdt[1] = dT_w;
			return dxdt;
			#endregion
		}
	}
}
