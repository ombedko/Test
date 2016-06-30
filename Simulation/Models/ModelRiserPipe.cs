using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumSimLib;

namespace SimulationGUI {
	class ModelRiserPipe:ODEModel {
		public List<double> lstWlOut = new List<double>();
		double fLastWlOut;

		public void Alg(double[] x, double[] u, out double wl_out, out double wg_out, out double wg_vv, out double Pb_r, out double Pt_r, out double Pg_ac) {
			double Cvv = 1e-4;     //[ms]                   Valve const virtual valve
			double Rho_l = 900;      //[kg/m^3]               Liquid density
			double Vg_ac = 48;       //[m^3]                  Volume of acumulated gas
			double R = 8.314;    //[J/mol*K]              Gas const
			double T = 363;      //[K]                    Const system temp
			double M = 2.2e-2;   //[kg/mol]               Molar weight of gas 
			double g = 9.81;     //[m/s^2]                gravityconst
			double thet = 3.14 / 4;     //[rad]                  angle

			double ml_min = 3.73e4;   //[kg]                   Min liquid mass in riser

			double A = 1.77e-2;  //[m^2]                  Cross-seciton area
			double L = 5200;     //[m]                    Riser Length
			double Vr = L * A;      // using L*A as total riser volume, different from Taskl description
									
			
			double Ps = 6.6e5;    //[N/m^2 = Pa]           Back presure separator
			double Cout = 2.8e-3;   //[m^2]                  Valve const topside
			

			

			//extract states
			double mg_ac = x[0];
			double mg_r = x[1];
			double ml_r = x[2];

			//algebraic equations (in order of aperance)        
			Pg_ac = (mg_ac * R * T) / (M * Vg_ac);                          //gas pressure accum.

			double Vgr = Vr - ((ml_r + ml_min) / Rho_l);                   //volume gas in riser
			Pt_r = (mg_r * R * T) / (M * Vgr);                            //gas pressure top riser
			Pb_r = Pt_r + (ml_r + ml_min) * (g * Math.Sin(thet) / A);    //gas pressure bottom riser  

			wg_vv = Cvv * Math.Max(0, (Pg_ac - Pb_r));                 //mass flowrate out of virtual valve    

			double w_out   = Cout * u[0] * Math.Sqrt(Rho_l * (Math.Max(0,Pt_r - Ps)));     //mass flowrate out of topside valve    
			wl_out  = (ml_r/(ml_r + mg_r)) * w_out;                     //mass liquid flow rate out of topside valve (assume liq dominates)
			wg_out  = (mg_r/(ml_r + mg_r)) * w_out;                     //mass gas flow rate out of topside valve

		}
		public override double[] dxdt(double[] x, double t, double[] u) {
			base.dxdt(x, t, u);
			
			//extract states
			double mg_ac = x[0];
			double mg_r = x[1];
			double ml_r = x[2];

			double lam = 0.78;     //[-]
			double wl_in = 11.75;    //[kg/s]                 const liq in-flow
			double wg_in = 8.2e-1;   //[kg/s]                 const gas in-flow

			double wl_out, wg_out, wg_vv, Pb_r, Pt_r, Pg_ac;

			Alg(x, u, out wl_out, out  wg_out, out wg_vv, out  Pb_r, out  Pt_r, out  Pg_ac);
		
			
			
			double[] dxdt = new double[3];
			dxdt[0] = (1-lam) * wg_in   - wg_vv;                    //mg_ac
			dxdt[1] = lam     * wg_in   + wg_vv     - wg_out;       //mg_r
			dxdt[2] =           wl_in               - wl_out;       //ml_r

			//save this here so it can be added to list of plotable values in EndStep
			fLastWlOut = wl_out;
			return dxdt;
		}
		public override void EndStep() {
			base.EndStep();
			lstWlOut.Add(fLastWlOut);
		}
	}
}
