//
// Program.cs
//
// Author:
//       Martin Haesemeyer <m.haesemeyer@gmail.com>
//
// Copyright (c) 2015 Martin Haesemeyer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
            labels.YLims = new Tuple<double, double>(0, 1.1);
            var Hist = py.MakeHistFunction(labels,true,true);
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
			py.Dispose();
            Console.WriteLine("Press return to exit");
            Console.ReadLine();
		}
	}
}
