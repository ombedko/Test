using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumSimLib;

namespace SimulationGUI {
	class ModelSingleZone:ODEModel {
		
		/// <summary>
		/// Ref Whatsala aritcle on single zone model
		/// </summary>		
		public override double[] dxdt(double[] x, double t, double[] u) {
			base.dxdt(x, t, u);

#region extract states and inputs from arguments
			//number of states, alocate return array
			int nx = 18;
			if(x.Length != nx)
				throw new Exception("Not enough states in x0. Found " + x.Length.ToString() + ", need " + nx.ToString());
			int nu = 10;
			if(u.Length != nu)
				throw new Exception("Not enough inputs in u. Found " + u.Length.ToString() + ", need " + nu.ToString());

			//EXTRACT STATES
			double Rho_b = x[0];
			double T_b	= x[1];
			
			double T_1w	= x[2];
			double T_2w	= x[3];
			double T_3w	= x[4];
			double T_4w	= x[5];
			
			double T_1f = x[6];
			double T_2f = x[7];
			double T_3f = x[8];

			double T_1r = x[9];
			double T_2r = x[10];
			double T_3r = x[11];
			double T_4r = x[12];

			double T_1fur = x[13];
			double T_2fur = x[14];
			double T_3fur = x[15];
			double T_4fur = x[16];
			double T_5fur = x[17];

			//EXTRACT INPUTS

			//heat sources
			double Qheater		= u[0];
			double Qpeople		= u[1];
			double Qappliences	= u[2];
			double Qsolar		= u[3];

			//floor heating
			double q_1f			= u[4];
			double q_2f			= u[5];
			double q_3f			= u[6];

			//outside weath condition parameters
			double T_inf		= u[7];
			double T_g			= T_inf + 5;

			//ventilation
			double N			= u[8];			
			double RH_out		= u[9];									
#endregion

#region MODEL			

			//CONSTANTS
			double R		= par("R");		//gas constant
			double M_air 	= par("M_air");	//Molar mass air
			double M_h2o	= par("M_h2o");	//Molar mass air
			double P0		= par("P0");
			double H_da_ref = par("H_da_ref");
			double H_wv_ref = par("H_wv_ref");

			//PARAMETERS

			//From Table 1
			double K_1w		= par("K_1w");		double K_2w		= par("K_2w");		double K_3w		= par("K_3w");		double K_4w		= par("K_4w");		// [W/(mk)]
			double K_1r		= par("K_1r");		double K_2r		= par("K_2r");		double K_3r		= par("K_3r");		double K_4r		= par("K_4r");		// [W/(mk)]
			double K_1f		= par("K_1f");		double K_2f		= par("K_2f");		double K_3f		= par("K_3f");											// [W/(mk)]
			double K_fur	= par("K_fur");																													// [W/(mk)]

			double a_1w		= par("a_1w");		double a_2w		= par("a_2w");		double a_3w		= par("a_3w");		double a_4w		= par("a_4w");		// [m2/s]
			double a_1r		= par("a_1r");		double a_2r		= par("a_2r");		double a_3r		= par("a_3r");		double a_4r		= par("a_4r");		// [m2/s]
			double a_1f		= par("a_1f");		double a_2f		= par("a_2f");		double a_3f		= par("a_3f");											// [m2/s]
			double a_fur	= par("a_fur");																													// [m2/s]

			double l_1w		= par("l_1w");		double l_2w		= par("l_2w");		double l_3w		= par("l_3w");		double l_4w		= par("l_4w");		// [m]
			double l_1r		= par("l_1r");		double l_2r		= par("l_2r");		double l_3r		= par("l_3r");		double l_4r		= par("l_4r");		// [m]
			double l_1f		= par("l_1f");		double l_2f		= par("l_2f");		double l_3f		= par("l_3f");											// [m]
				
			double Rho_1f	= par("Rho_1f");	double Rho_2f	= par("Rho_2f");	double Rho_3f	= par("Rho_3f");										// [kg/m3]
			double cp_1f	= par("cp_1f");		double cp_2f	= par("cp_2f");		double cp_3f	= par("cp_3f");											// [J/(kg*K)]

			//UA values (thermal resistance)
			double Awindow	= par("Awindow");
			double Adoor	= par("Adoor");
			double Awalls	= par("Awalls");
			double Afloor	= par("Afloor");
			double Aroof	= par("Aroof");
			double Afur		= par("Afur");
			
			//From Table 2
			double h_bw		= par("h_bw");
			double h_infw	= par("h_infw");

			double h_br		= par("h_br");
			double h_infr	= par("h_infr");

			double h_bf		= par("h_bf");
			double h_bfur	= par("h_bfur");

			double Uwindow	= par("Uwindow");
			double Udoor	= par("Udoor");
			double Uwalls	= par("Uwalls");
			double Uroof	= par("Uroof");
			double Ufloor	= par("Ufloor");

			double dr_fur	= par("dr_fur");
			double r_fur	= par("r_fur");
			
			
			//building
			double V_b		= par("V_b");			
			
			//ventilation
			double c_pa		= par("c_pa");
			double c_pw		= par("c_pw");
			double h_we		= par("h_we");

			//surface tempratures of layers
			
			double Ts_2w = (T_1w + T_2w) / 2.0;
			double Ts_3w = (T_2w + T_3w) / 2.0;
			double Ts_4w = (T_3w + T_4w) / 2.0;
			double Ts_1w = (h_bw * T_b   + (K_1w / (2 * l_1w)) * Ts_2w)     /   (h_bw + K_1w/(2*l_1w));				//eq 8, solved for Ts_1w
			double Ts_5w = (h_infw * T_inf + (K_4w / (2 * l_4w)) * Ts_4w)   /   (h_infw + K_4w/(2*l_4w));			//eq 9, solved for Ts_5w

			
			double Ts_2r = (T_1r + T_2r) / 2.0;
			double Ts_3r = (T_2r + T_3r) / 2.0;
			double Ts_4r = (T_3r + T_4r) / 2.0;
			double Ts_1r = (h_br * T_b   + (K_1r / (2 * l_1r)) * Ts_2r)     /   (h_br + K_1r / (2 * l_1r));			//eq 13, solved for Ts_1r
			double Ts_5r = (h_infr * T_inf + (K_4r / (2 * l_4r)) * Ts_4r)   /   (h_infr + K_4r / (2 * l_4r));		//eq 14, solved for Ts_1r

			
			double Ts_2f = (T_1f + T_2f) / 2.0;
			double Ts_3f = (T_2f + T_3f) / 2.0;
			double Ts_1f = (h_bf * T_b + (K_1f / (2 * l_1f)) * Ts_2f)       /   (h_bf + K_1f / (2 * l_1f));			//eq 11, solved for Ts_1f
			double Ts_4f = (Ts_3f + T_g) / 2.0;

			
			double Ts_2fur = (T_1fur + T_2fur) / 2.0;
			double Ts_3fur = (T_2fur + T_3fur) / 2.0;
			double Ts_4fur = (T_3fur + T_4fur) / 2.0;
			double Ts_5fur = (T_4fur + T_5fur) / 2.0;
			double Ts_1fur = (h_bfur * T_b + (K_fur / (2 * dr_fur)) * Ts_2fur) / (h_bfur + K_fur / (2 * dr_fur));
			double Ts_6fur = T_5fur;

			double T_center = (T_1fur+T_b)/2;	//center of furniture, equal Ts_6fur?
			
			//Compute HEAT FLUX, eq 17-22
			double Qwindow	= Uwindow * Awindow	* (T_b - T_inf);		//eq17
			double Qdoor	= Udoor * Adoor		* (T_b - T_inf);		//eq18
			double Qwalls	= Uwalls * Awalls	* (T_b - T_inf);		//eq19
			double Qfloor	= Ufloor * Afloor	* (T_b - T_g);			//eq20
			double Qroof	= Uroof * Aroof		* (T_b - T_inf);		//eq21
			double Qfur		= h_bfur * Afur		* (T_b - T_center);		//eq22
						
			double Qsupply	= Qheater + Qpeople + Qappliences + Qsolar;				//eq 6, sum inputs
			double Qloss	= Qwindow + Qdoor + Qwalls + Qfloor + Qroof + Qfur;
			double Q		= Qsupply - Qloss;

			//compute ventilation
			//Afloor = (4-0.4)*(3.65-0.4);			// [m2] Area of the Floor
			//V_b = (4-0.4)*(3.65-0.4)*(3.3-0.2353);  // [m3] Volume of the room
			double V_in = N*Afloor/3600;			// [m3/S] Inlet air flow rate
			double V_out = N*Afloor/3600;			// [m3/S] Outlet air flow rate


			// Coefficients to determine the saturation vapor pressure of water at at a certain temperature
			double[] pc = new double[]{5.2623e-09, -6.3323e-06, 0.003072,-0.75032,92.195,-4556.2,91.59};
			//double Psat = pc[0] * T_inf ^ 6 + pc[1] * T_inf ^ 5 + pc[2] * T_inf ^ 4 + pc[3] * T_inf ^ 3 + pc[4] * T_inf ^ 2 + pc[5] * T_inf + pc[6]; // [Pa] Saturation pressure of water at considering temperature and pressure
			double Psat = 0;
			for(int i = 0; i < 7; i++)
				Psat += pc[i] * Math.Pow(T_inf+273.15, 6 - i);


			//compute molar mass of building air + water
			double PH2O = RH_out * Psat;
			double f_h2o_in = PH2O / P0;
			double M_b = M_air * (1 - f_h2o_in) + M_h2o * f_h2o_in;

			//compute helping attributes 
			double H_fg = 4.0656e4 / M_h2o;
			double xi = PH2O * M_h2o / (PH2O * M_h2o + (P0 - PH2O) * M_air);
			double Rho_in = P0 * M_b / (R * (T_inf+273.15));  
			double xo = (N > 0 ? V_in * Rho_in * xi / (V_out * Rho_b) : Rho_in * xi / Rho_b);
			double Ti = T_inf - 25;
			double T = T_b - 25;

			//compute specific heat of air, and specific enthalpyu of air
			double h_in		= (1 - xi) * ((29.13 * Ti) / M_air + H_da_ref)    +    xi * (H_wv_ref + H_fg + (32.24 * Ti  + 1.924e-3 * Ti*Ti   / 2 + 1.055e-5 * Ti*Ti*Ti    / 3 - 3.596e-9 * Ti*Ti*Ti*Ti     / 4) / M_h2o);		// [J/kg] Specific enthalpy of inlet air
			double h_out	= (1 - xo) * ((29.13 * T)  / M_air + H_da_ref)    +    xo * (H_wv_ref + H_fg + (32.24 * T   + 1.924e-3 * T*T     / 2 + 1.055e-5 * T*T*T       / 3 - 3.596e-9 * T*T*T*T         / 4) / M_h2o);		// [J/kg] Specific enthalpy of outlet air
			T_b += 273.15;
			double cp_b		= (1 - xo) *   29.13       / M_air                +    xo * (                  (32.24 * T_b + 1.924e-3 * T_b*T_b / 2 + 1.055e-5 * T_b*T_b*T_b / 3 - 3.596e-9 * T_b*T_b*T_b*T_b / 4) / M_h2o);		// [J/kg] Specific heat capacity of moist air in the room
			T_b -= 273.15;

			//DIFFERENTIAL EQUATIONS
			//building				
			double dRho_b	= (V_in * Rho_in - V_out * Rho_b) / V_b;																							//eq 1

			//eq 5
			double dT_b1	= (V_in * Rho_in * h_in - V_out * Rho_b * h_out) / (V_b * Rho_b * (cp_b - (R / M_b)));
			double dT_b2	= -T_b * (V_out * Rho_b - V_in * Rho_in) / (V_b * Rho_b);
			double dT_b3	= Q / (V_b * Rho_b * (cp_b - (R / M_b)));

			//wall
			double dT_1w	= a_1w * (Ts_2w - 2 * T_1w + Ts_1w) / (l_1w * l_1w);																				//eq 7
			double dT_2w	= a_2w * (Ts_3w - 2 * T_2w + Ts_2w) / (l_2w * l_2w);					
			double dT_3w	= a_3w * (Ts_4w - 2 * T_3w + Ts_3w) / (l_3w * l_3w);
			double dT_4w	= a_4w * (Ts_5w - 2 * T_4w + Ts_4w) / (l_4w * l_4w);

			//floor w/ heater q_if
			double dT_1f	= a_1f * (Ts_2f - 2 * T_1f + Ts_1f) / (l_1f * l_1f)   +   q_1f / (Rho_1f * cp_1f);													//eq 10
			double dT_2f	= a_2f * (Ts_3f - 2 * T_2f + Ts_2f) / (l_2f * l_2f)	  +   q_2f / (Rho_2f * cp_2f);
			double dT_3f	= a_3f * (Ts_4f - 2 * T_3f + Ts_3f) / (l_3f * l_3f)   +   q_3f / (Rho_3f * cp_3f);

			//roof
			double dT_1r	= a_1r * (Ts_2r - 2 * T_1r + Ts_1r) / (l_1r * l_1r);																				//eq 12
			double dT_2r	= a_2r * (Ts_3r - 2 * T_2r + Ts_2r) / (l_2r * l_2r);
			double dT_3r	= a_3r * (Ts_4r - 2 * T_3r + Ts_3r) / (l_3r * l_3r);
			double dT_4r	= a_4r * (Ts_5r - 2 * T_4r + Ts_4r) / (l_4r * l_4r);

			//furniture
			double dT_1fur = a_fur * (Ts_2fur - 2 * T_1fur + Ts_1fur) / (dr_fur * dr_fur)   +   (a_fur) * (1/(9*dr_fur)) * (Ts_2fur - Ts_1fur) / dr_fur;		//eq 15																			//eq 16
			double dT_2fur = a_fur * (Ts_3fur - 2 * T_2fur + Ts_2fur) / (dr_fur * dr_fur)   +   (a_fur) * (1/(7*dr_fur)) * (Ts_3fur - Ts_2fur) / dr_fur;							 
			double dT_3fur = a_fur * (Ts_4fur - 2 * T_3fur + Ts_3fur) / (dr_fur * dr_fur)   +   (a_fur) * (1/(5*dr_fur)) * (Ts_4fur - Ts_3fur) / dr_fur;
			double dT_4fur = a_fur * (Ts_5fur - 2 * T_4fur + Ts_4fur) / (dr_fur * dr_fur)   +   (a_fur) * (1/(3*dr_fur)) * (Ts_5fur - Ts_4fur) / dr_fur;
			double dT_5fur = a_fur * (Ts_6fur - 2 * T_5fur + Ts_5fur) / (dr_fur * dr_fur)   +   (a_fur) * (1/(1*dr_fur)) * (Ts_6fur - Ts_5fur) / dr_fur;					

#endregion

#region return differentials	
			//RETURN DIFFERENTIALS
			double[] dxdt = new double[nx];
			dxdt[0]		= dRho_b;
			dxdt[1]		= dT_b1 + dT_b2 + dT_b3;

			dxdt[2]		= dT_1w;
			dxdt[3]		= dT_2w;
			dxdt[4]		= dT_3w;
			dxdt[5]		= dT_4w;

			dxdt[6]		= dT_1f;
			dxdt[7]		= dT_2f;
			dxdt[8]		= dT_3f;

			dxdt[9]		= dT_1r;
			dxdt[10]	= dT_2r;
			dxdt[11]	= dT_3r;
			dxdt[12]	= dT_4r;

			dxdt[13]	= dT_1fur;
			dxdt[14]	= dT_2fur;
			dxdt[15]	= dT_3fur;
			dxdt[16]	= dT_4fur;
			dxdt[17]	= dT_5fur;
			
			return dxdt;
#endregion
		}
		
	}
}
