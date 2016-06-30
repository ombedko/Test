using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumSimLib;
using NumSimLib.Solver;

namespace SimulationGUI {
	class Factory {
		public static ODEModel MakeModel(ModelType model){
			switch(model) {
				case ModelType.LP:
					return new ModelLP();
				case ModelType.SingleZone:
					return new ModelSingleZone();
				case ModelType.R4C2:
					return new ModelR4C2();
				case ModelType.R4C2_ONOFF:
					return new ModelR4C2_ONOFF();
				case ModelType.R6C2:
					return new ModelR6C2();
				case ModelType.R6C3:
					return new ModelR6C3();
				case ModelType.R7C3:
					return new ModelR7C3();
				case ModelType.R5C3:
					return new ModelR5C3();
			}
			return null;
		}
		public static ODESolver MakeSolver(SolverType solver) {
			switch(solver) {
				case SolverType.FE:
					return new SolverFE();
				case SolverType.Heun:
					return new SolverHeun();
				case SolverType.RK4:
					return new SolverRK4();
			}
			return null;
		}
	}
}
