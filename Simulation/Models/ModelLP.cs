using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumSimLib;

namespace SimulationGUI {
	class ModelLP:ODEModel {
		public override double[] dxdt(double[] x, double t, double[] u) {
			base.dxdt(x, t, u);

			//get parameters
			double T = par("T");

			int nLen = Math.Min(x.Length,u.Length);

			double[] dxdt = new double[nLen];
			for(int i = 0; i < nLen;i++)
				dxdt[i] = 1 / T * (u[i] - x[i]);			

			return dxdt;
		}
	}
}
