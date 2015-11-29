//
// PyPlotInterface.cs
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
using System.IO;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace PythonInterface
{

    /// <summary>
    /// Defines the most common plot labels and modifiers
    /// </summary>
    public struct PlotDecorators
    {
        public string Title;

        public string XLabel;

        public string YLabel;

        public Tuple<double,double> XLims;

        public Tuple<double,double> YLims;

        public PlotDecorators(string title, string xlabel, string ylabel)
        {
            Title = title;
            XLabel = xlabel;
            YLabel = ylabel;
            XLims = null;
            YLims = null;
        }
    }

    /// <summary>
    /// Represents the color of a plot element in RGB
    /// values btw. 0 and 1 inclusive
    /// </summary>
    public struct PlotColor
    {
        private double[] _rgb;

        public double R
        {
            get
            {
                return _rgb[0];
            }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("R", "Color value has to be btw. 0 and 1");
                if (_rgb == null)
                    _rgb = new double[3];
                _rgb[0] = value;
            }
        }

        public double G
        {
            get
            {
                return _rgb[1];
            }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("G", "Color value has to be btw. 0 and 1");
                if (_rgb == null)
                    _rgb = new double[3];
                _rgb[1] = value;
            }
        }

        public double B
        {
            get
            {
                return _rgb[2];
            }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("B", "Color value has to be btw. 0 and 1");
                if (_rgb == null)
                    _rgb = new double[3];
                _rgb[2] = value;
            }
        }

        internal PyTuple<double> AsPyTuple(string varName = "")
        {
            if (_rgb == null)
                _rgb = new double[3];
            return new PyTuple<double>(_rgb, varName);
        }

        public PlotColor(double r, double g, double b)
        {
            _rgb = new double[3];
            R = r;
            G = g;
            B = b;
        }
    }







	/// <summary>
	/// Simple process interface to python for drawing plots
	/// using matplotlib.
	/// </summary>
	public sealed class PyPlotInterface : IDisposable
	{
		/// <summary>
		/// Seaborn axes styles
		/// </summary>
		public enum AxesStyle{white, dark, whitegrid, darkgrid, ticks};

		#region Members

        /// <summary>
        /// The python interpreter process.
        /// </summary>
		private Process _pyInterp;

        /// <summary>
        /// The start infor for the python process
        /// </summary>
		private ProcessStartInfo _pySI;

		/// <summary>
		/// The current indentation level
		/// </summary>
		private int _indent;

        /// <summary>
        /// Internal counter to create unique
        /// variables representing figures.
        /// </summary>
        private int _figNum;

		private string Indent
		{
			get
			{
				if (_indent == 0)
					return "";
				StringBuilder sb = new StringBuilder(_indent * 2);
				for (int i = 0; i < _indent; i++)
					sb.Append("  ");
				return sb.ToString();
			}
		}

		/// <summary>
		/// Determines whether we use seaborn
		/// </summary>
		private bool _useSeaborn;

		/// <summary>
		/// If set to true, write some of Pythons Std-error stream to the console
		/// </summary>
		private readonly bool _debug;

		/// <summary>
		/// The numpy library import prefix
		/// </summary>
		public const string NP = "np";

		/// <summary>
		/// The matplotlib.pyplot import prefix
		/// </summary>
		public const string PL = "pl";

		/// <summary>
		/// The seaborn import prefix
		/// </summary>
		public const string SNS = "sns";

		#endregion

		/// <summary>
		/// Creates a new PyPlotInterface using seaborn and omitting python debug output
		/// </summary>
		public PyPlotInterface() : this(true,false){}

		/// <summary>
		/// Creates a new PyPlotInterface
		/// </summary>
		/// <param name="useSeaborn">If set to <c>true</c> use seaborn.</param>
		public PyPlotInterface(bool useSeaborn) : this(useSeaborn,false,"python"){}

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonProcess.PyPlotInterface"/> class.
        /// </summary>
        /// <param name="useSeaborn">If set to <c>true</c> use seaborn.</param>
        /// <param name="debug">If set to <c>true</c> write python errors to console.</param>
        public PyPlotInterface(bool useSeaborn, bool debug) : this(useSeaborn,debug,"python"){}

		/// <summary>
		/// Initializes a new instance of the <see cref="PythonProcess.PyPlotInterface"/> class.
		/// </summary>
		/// <param name="useSeaborn">If set to <c>true</c> use seaborn.</param>
		/// <param name="debug">If set to <c>true</c> write python errors to console.</param>
        /// <param name="pythonInterpreter">Path and name of python interpreter.</param>
        public PyPlotInterface(bool useSeaborn, bool debug, string pythonInterpreter)
		{
			_debug = debug;
			_indent = 0;
            _figNum = 0;
			//Configure and start python interpreter
			//the -i option is necessary as python otherwise thinks it interacts with a script
			//the -u option is for some reason necessary to allow multiple plots to appear ??
			//maybe there are some internal buffering issues that -u removes
            _pySI = new ProcessStartInfo(pythonInterpreter,"-i -u");
			_pySI.UseShellExecute = false;//required to redirect standard streams
			_pySI.CreateNoWindow = true;//don't create separate window
			_pySI.RedirectStandardInput = true;//let us do the talking
			_pySI.RedirectStandardError = true;
			_pySI.RedirectStandardOutput = true;

			_pyInterp = new Process();
			_pyInterp.StartInfo = _pySI;

			_pyInterp.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
				{OutputFilter(e.Data);
				});

			_pyInterp.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
				{	var filt = ErrorFilter(e.Data);
					if(filt.Length>0 && _debug)
						Console.WriteLine(filt);
				});
			

			//start process
			_pyInterp.Start();
			_pyInterp.BeginOutputReadLine();
			_pyInterp.BeginErrorReadLine();
			//perform standard imports
			WriteLine("import matplotlib");
            //TODO: Offer backend choice?
			WriteLine("matplotlib.use('Qt4Agg')");
            //WriteLine("matplotlib.use('TkAgg')");
			WriteLine("import numpy as {0}",NP);
			WriteLine("import matplotlib.pyplot as {0}", PL);
			if (useSeaborn)
			{
				_useSeaborn = true;
				WriteLine("import seaborn as {0}", SNS);
			}
			//make plotting interactive
			WriteLine("from matplotlib import interactive");
			WriteLine("interactive(True)");
			Flush();

		}

		#region Methods

		/// <summary>
		/// Writes formatted text to the standard input of the python process
		/// </summary>
		/// <param name="format">The format string</param>
		/// <param name="arg">Argument list to replace format characters in format</param>
		private void Write(string format, params object[] arg)
		{
			if(_pyInterp != null)
				_pyInterp.StandardInput.Write(Indent+format, arg);
		}

		/// <summary>
		/// Writes text to the standard input of the python process
		/// </summary>
		/// <param name="text">The text to write</param>
		private void Write(string text)
		{
			if(_pyInterp != null)
				_pyInterp.StandardInput.Write(Indent+text);
		}

		/// <summary>
		/// Writes one line to the standard input of the python process
		/// </summary>
		/// <param name="format">The format string</param>
		/// <param name="arg">Argument list to replace format characters in format</param>
		private void WriteLine(string format, params object[] arg)
		{
			if(_pyInterp != null)

				_pyInterp.StandardInput.WriteLine(Indent+format, arg);
		}

		/// <summary>
		/// Writes one line to the standard input of the python process
		/// </summary>
		/// <param name="text">The text to write</param>
		private void WriteLine(string text)
		{
			if(_pyInterp != null)
				_pyInterp.StandardInput.WriteLine(Indent+text);
		}

        /// <summary>
        /// Flushes the standard input.
        /// </summary>
		private void Flush()
		{
			if (_pyInterp != null)
				_pyInterp.StandardInput.Flush();
		}

		/// <summary>
		/// Proccesses lines received on the standard error stream
		/// </summary>
		/// <returns>Text that should be written back to the command window</returns>
		private string ErrorFilter(string errorLine)
		{
			if (errorLine.Contains("Python "))
			{
				return errorLine;
			}
			else if (errorLine.Contains("Error") || errorLine.Contains("error"))
			{
				//check if an attempted seaborn import failed and react accordingly
				if (errorLine.Contains("ImportError") && errorLine.Contains("seaborn"))
					_useSeaborn = false;
				return errorLine;
			}
			return "";
		}

		/// <summary>
		/// Processes lines received on the standard out stream
		/// </summary>
		/// <param name="outputLine">Output line.</param>
		private void OutputFilter(string outputLine)
		{
			
		}

		/// <summary>
		/// Terminates indendation, telling the interpreter that
		/// indented block has been left and execution can resume
		/// </summary>
		private void TerminateIndent()
		{
			//this is necessary to signal to the interpreter that we left indented block!
			while(_indent>0)
			{
				_indent--;
				WriteLine("");
			}
		}

		/// <summary>
		/// Sets the plot style if seaborn is present
		/// </summary>
		private void SetAxesStyle(AxesStyle style = AxesStyle.whitegrid)
		{
			if(_useSeaborn)
			{
				WriteLine("with {0}.axes_style('{1}'):",SNS,style.ToString());
				_indent++;
			}
		}

		/// <summary>
		/// Despines the current axis.
		/// </summary>
		private void Despine()
		{
			if(_useSeaborn)
				WriteLine("sns.despine()");
		}

		/// <summary>
		/// Decorates a plot by adding title and labels, limits, etc.
		/// </summary>
		/// <param name="PlotLabels">The plot labels</param>
        private void Decorate(PlotDecorators plotLabels)
		{
            if(plotLabels.XLabel!="")
                Write("ax.set_xlabel('{0}');",plotLabels.XLabel);
            if(plotLabels.YLabel!="")
                Write("ax.set_ylabel('{0}');",plotLabels.YLabel);
            if(plotLabels.Title!="")
                Write("ax.set_title('{0}');",plotLabels.Title);
            if (plotLabels.XLims != null)
                Write("ax.set_xlim({0},{1});", plotLabels.XLims.Item1, plotLabels.XLims.Item2);
            if (plotLabels.YLims != null)
                Write("ax.set_ylim({0},{1});", plotLabels.YLims.Item1, plotLabels.YLims.Item2);
		}

		/// <summary>
		/// Ends the line of plotting commands adding figure re-fresh
		/// </summary>
        private void EndDrawCommands(string figureName)
		{
			WriteLine(figureName+".canvas.draw_idle()");
		}

		/// <summary>
		/// Creates a new figure and (sub)plot
		/// </summary>
		/// <param name="nrows">Number of rows on the plot grid</param>
		/// <param name="ncols">Number of columns on the plot grid</param>
        /// <returns>>The name of the figure variable</returns>
        private string Subplots(int nrows=1,int ncols=1)
		{
			if (nrows < 1)
				throw new ArgumentOutOfRangeException("nrows", "nrows has to be 1 or larger");
			if (ncols < 1)
				throw new ArgumentOutOfRangeException("ncols", "ncols has to be 1 or larger");
			if (nrows > 1 || ncols > 1)
				throw new NotSupportedException("Currently multiple subplots are not supported!");
            string figureName = string.Format("fig_{0}", _figNum++);
            Write("{0}, ax = {1}.subplots(nrows={2},ncols={3});",figureName,PL,nrows,ncols);
            return figureName;
		}

		/// <summary>
		/// Transfers a 1D array of doubles to python by creating
		/// a corresponding numpy array
		/// </summary>
		/// <returns>The python name of the created array</returns>
		/// <param name="d">The data to transfer</param>
		private string Transfer1DArray(double[] d)
		{
			var data = new NumpyArray1D(d);
			WriteLine(data.EncodingStatement);
			return data.VarName;
		}

        /// <summary>
        /// Transfers a scalar to the python process
        /// </summary>
        /// <returns>The python name of the scalar</returns>
        /// <param name="v">The value to transfer</param>
        private string TransferValue(double v)
        {
            var value = new PyScalar<double>(v);
            WriteLine(value.EncodingStatement);
            return value.VarName;
        }

        /// <summary>
        /// Transfers a scalar to the python process
        /// </summary>
        /// <returns>The python name of the scalar</returns>
        /// <param name="v">The value to transfer</param>
        private string TransferValue(int v)
        {
            var value = new PyScalar<int>(v);
            WriteLine(value.EncodingStatement);
            return value.VarName;
        }

		/// <summary>
		/// Transfers one or two variables to the python process and adds
		/// it to the current plotting calls.
		/// </summary>
		/// <param name="x">The x coordinates</param>
		/// <param name="y">The y coordinates</param>
		private void CallPlot(double[] x, double[] y)
		{
			if (x == null)
				throw new ArgumentNullException("x", "X series has to exist");
			if (y != null && y.Length != x.Length)
				throw new ArgumentException("If y series is present it needs to have same length as x series");
			//create numpy array objects in python process
			string x_name = Transfer1DArray(x);
			//all commands dealing with single figure need to occur on one line
			//otherwise matplotlib does not update the canvas...
			if (y != null)
			{
				string y_name = Transfer1DArray(y);
				Write("ax.plot({0},{1});", x_name, y_name);
			}
			else
				Write("ax.plot({0});", x_name);
		}

        /// <summary>
        /// Transfers data and bin count to the python process and
        /// adds histogram to the current plotting calls.
        /// </summary>
        /// <param name="data">The histogram data</param>
        /// <param name="nBins">The desired number of bins</param>
        /// <param name="normalize">If set to <c>true</c> normalize histogram.</param>
        /// <param name="cumulative">If set to <c>true</c> plot cumulative histogram</param>
        private void CallHist(double[] data, int nBins, bool normalize, bool cumulative)
        {
            if (data == null)
                throw new ArgumentNullException("data", "Data series has to exist");
            if (nBins < 1)
                throw new ArgumentOutOfRangeException("nBins", "nBins has to be >=1");
            string d_name = Transfer1DArray(data);
            string b_name = TransferValue(nBins);
            Write("ax.hist({0},{1},normed={2},cumulative={3});", d_name, b_name,
                normalize ? "True" : "False", cumulative ? "True" : "False");
        }

		/// <summary>
		/// Makes an x,y line plot function.
		/// </summary>
		/// <returns>The plot function which takes an x and optionally 
        /// y series and returns the figure varible name.</returns>
        /// <param name="plotLabels">The labeling of the plot</param>
        /// <param name="AxesStyle">Seaborn axes plot style</param>
		/// <param name="despine">If set to <c>true</c> despine the plot using seaborn.</param>
        public Func<double[], double[], string> MakePlotFunction(PlotDecorators plotLabels 
            ,AxesStyle gridStyle = AxesStyle.whitegrid, bool despine=true)
		{
			return (x, y) =>
			{
				if(IsDisposed)
					throw new ObjectDisposedException("PyPlotInterface");
				if (x == null)
					throw new ArgumentNullException("x", "X series has to exist");
				if (y != null && y.Length != x.Length)
					throw new ArgumentException("If y series is present it needs to have same length as x series");
				

				//plot
				SetAxesStyle(gridStyle);//sets the plotting style
				string figName = Subplots();//creates figure and axis
				CallPlot(x,y);//plots the data on the axis object
                Decorate(plotLabels);//adds title and axis label decorations
                EndDrawCommands(figName);//forces figure refresh and terminates the plot commands line
				if(despine)
				{
					Despine();//uses seaborn to remove the top and right spine
				}
				TerminateIndent();//leaves the indented block structure
				Flush();//forces transfer to python process
                return figName;
			};
		}

        /// <summary>
        /// Makes a plot function for multiple x/y series.
        /// </summary>
        /// <returns>The plot function which takes multiple x and optionall y series
        /// and returns the figure variable name</returns>
        /// <param name="plotLabels">The labeling of the plot</param>
        /// <param name="AxesStyle">Seaborn axes plot style</param>
        /// <param name="despine">If set to <c>true</c> despine the plot using seaborn.</param>
        public Func<List<double[]>,List<double[]>,string> MakeSeriesPlotFunction(PlotDecorators plotLabels 
            ,AxesStyle gridStyle = AxesStyle.whitegrid, bool despine=true)
		{
			return (list_x, list_y) =>
			{
				if(IsDisposed)
					throw new ObjectDisposedException("PyPlotInterface");
				if (list_x == null)
					throw new ArgumentNullException("x", "X series have to exist");
				if (list_y != null && list_x.Count != 1 && list_y.Count!=list_x.Count)

					throw new ArgumentException("If y series are present there either needs to be exactly" +
						"one x-series or one x-series object per y series!");

				//make sure that all corresponding series have the same length
				if(list_y != null)
				{
					if(list_x.Count==1)
					{
						foreach(double[] y in list_y)
							if(y.Length != list_x[0].Length)
								throw new ArgumentException("At least on y-series did not have same length as x-series");
					}
					else
					{
						for(int i = 0;i<list_x.Count;i++)
							if(list_x[i].Length != list_y[i].Length)
								throw new ArgumentException("At least on y-series did not have same length as corresponding x-series");
					}
				}


				//plot
				SetAxesStyle(gridStyle);//sets the plotting style
				string figName = Subplots();//creates figure and axis
				if(list_y == null)
				{
					foreach(var x in list_x)
						CallPlot(x,null);
				}
				else
				{
					if(list_x.Count==1)
						foreach(var y in list_y)
							CallPlot(list_x[0],y);
					else
						for(int i=0;i<list_x.Count;i++)
							CallPlot(list_x[i],list_y[i]);
				}
                Decorate(plotLabels);//adds title and axis label decorations
                EndDrawCommands(figName);//forces figure refresh and terminates the plot commands line
				if(despine)
				{
					Despine();//uses seaborn to remove the top and right spine
				}
				TerminateIndent();//leaves the indented block structure
				Flush();//forces transfer to python process
                return figName;
			};
		}

        /// <summary>
        /// Makes a function to plot a histogram with a given number of bins.
        /// </summary>
        /// <returns>The hist function which takes the individual data values as well
        /// as the number of desired bins and returns the figure variable name.</returns>
        /// <param name="plotLabels">The labeling of the plot</param>
        /// <param name="normalize">If true, histogram will be normalized to sum 1</param> 
        /// <param name="gridStyle">Grid style.</param>
        /// <param name="despine">If set to <c>true</c> despine.</param>
        public Func<double[],int,string> MakeHistFunction(PlotDecorators plotLabels, bool normalize = false
            ,bool cumulative = false, AxesStyle gridStyle = AxesStyle.whitegrid, bool despine=true)
        {
            return (x, nbins) =>
            {
                if (IsDisposed)
                    throw new ObjectDisposedException("PyPlotInterface");
                if (x == null)
                    throw new ArgumentNullException("x", "Data series has to exist");
                if (nbins < 1)
                    throw new ArgumentOutOfRangeException("nbins", "nbins has to be >=1");
                //plot
                SetAxesStyle(gridStyle);//sets the plotting style
                string figName = Subplots();//creates figure and axis
                CallHist(x,nbins,normalize,cumulative);//draw histogram
                Decorate(plotLabels);//adds title and axis label decorations
                EndDrawCommands(figName);//forces figure refresh and terminates the plot commands line
                if(despine)
                {
                    Despine();//uses seaborn to remove the top and right spine
                }
                TerminateIndent();//leaves the indented block structure
                Flush();//forces transfer to python process
                return figName;
            };
        }

        /// <summary>
        /// Closes the specified figure.
        /// </summary>
        /// <param name="figName">Figure name.</param>
        public void CloseFigure(string figName)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("PyPlotInterface");
            WriteLine("{0}.close({1})", PL, figName);
        }

        /// <summary>
        /// Closes all figures.
        /// </summary>
        public void CloseAllFigures()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("PyPlotInterface");
            WriteLine("{0}.close('all')", PL);
        }

		#endregion

		#region DisposableSupport

        /// <summary>
        /// Indicates whether this instance is disposed
        /// </summary>
        /// <value><c>true</c> if this instance is disposed; otherwise, <c>false</c>.</value>
		public bool IsDisposed{ get; private set; }

        /// <summary>
        /// Free unmanaged resources of this process
        /// </summary>
        /// <param name="disposing">If set to <c>true</c> disposing.</param>
		private void Dispose(bool disposing)
		{
			if (disposing) 
			{
				WriteLine("quit()");
				Thread.Sleep(500);
				//clean up resources here
				if (_pyInterp != null) {
					if (_pyInterp.WaitForExit(10000))
						Console.WriteLine("Exited");
					else
						Console.WriteLine("Did not exit");
					_pyInterp.Close();
					_pyInterp.Dispose();
				}
			}
		}

        /// <summary>
        /// Releases all resource used by the <see cref="PythonInterface.PyPlotInterface"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="PythonInterface.PyPlotInterface"/>.
        /// The <see cref="Dispose"/> method leaves the <see cref="PythonInterface.PyPlotInterface"/> in an unusable
        /// state. After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="PythonInterface.PyPlotInterface"/> so the garbage collector can reclaim the memory that the
        /// <see cref="PythonInterface.PyPlotInterface"/> was occupying.</remarks>
		public void Dispose()
		{
			if (IsDisposed)
				return;
			IsDisposed = true;
			GC.SuppressFinalize(this);
			Dispose(true);
		}

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="PythonInterface.PyPlotInterface"/> is reclaimed by garbage collection.
        /// </summary>
		~PyPlotInterface()
		{
			if(!IsDisposed)
				Dispose(false);
		}

		#endregion
	}
}

