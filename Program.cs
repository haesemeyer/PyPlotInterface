//
//  Author:
//    Martin Haesemeyer m.haesemeyer@gmail.com
//
//  Copyright (c) 2015, Martin Haesemeyer
//
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using PythonInterface;

namespace PyPlotTest
{
    /// <summary>
    /// Command line program to test PyPlotInterface
    /// </summary>
	class MainClass
	{

		public static void Main (string[] args)
		{
			var py = new PyPlotInterface(true,true);
            var labels = new PlotDecorators();
            labels.Title = "Test of single plot";
            labels.XLabel = "X";
            labels.YLabel = "Y";
            var Plot = py.MakePlotFunction(labels, PyPlotInterface.AxesStyle.dark);
            labels.Title = "Test of histogram plot";
            labels.YLims = new Tuple<double, double>(0, 600);
            var Hist = py.MakeHistFunction(labels);
            labels.Title = "Test of series plot";
            labels.YLims = new Tuple<double, double>(-5, 5);
            var PlotMulti = py.MakeSeriesPlotFunction(labels);

			//create our plot data
			int dataSize = 5000;
			double[] x = new double[dataSize];
			double[] y = new double[dataSize];
			double[] y2 = new double[dataSize];
            double[] y3 = new double[dataSize];
			for (int i = 0; i < dataSize; i++)
			{
				x[i] = 10.0 / dataSize * i;
				y[i] = Math.Sin(x[i]);
				y2[i] = Math.Cos(x[i]);
                y3[i] = Math.Tan(x[i]);
			}
			List<double[]> X = new List<double[]>();
			X.Add(x);
			X.Add(y);
			X.Add(x);
            X.Add(x);
			List<double[]> Y = new List<double[]>();
			Y.Add(y);
			Y.Add(y2);
			Y.Add(y2);
            Y.Add(y3);
			Console.WriteLine("Plotting");
			PlotMulti(X, Y);
			Plot(y, x);
			string f3 = Hist(y, 20);
            Console.WriteLine("Press return to close figure 3");
            Console.ReadLine();
            py.CloseFigure(f3);
			Console.WriteLine("Press return to close remaining figures");
			Console.ReadLine();
            py.CloseAllFigures();
            Console.WriteLine("Press return to exit");
            Console.ReadLine();
			py.Dispose();
		}
	}
}
