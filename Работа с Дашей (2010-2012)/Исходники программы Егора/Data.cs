using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;
using System.Threading;
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Reflection;

namespace Data
{
    public class Experiment_Data
    {
        #region variables and consts

        #region path
        private string path;

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        #endregion

        #region values
        private SortedDictionary<int, double> values = new SortedDictionary<int, double>();

        public SortedDictionary<int, double> Values
        {
            get { return values; }
            set { values = value; }
        }

        private SortedDictionary<int, double> sleekmass;

        public SortedDictionary<int, double> SleekMass
        {
            get { return sleekmass; }
            set { sleekmass = value; }
        }

        private SortedDictionary<int, double> rate;

        public SortedDictionary<int, double> Rate
        {
            get { return rate; }
            set { rate = value; }
        }

        private SortedDictionary<int, double> smoothedrate;

        public SortedDictionary<int, double> SmoothedRate
        {
            get { return smoothedrate; }
            set { smoothedrate = value; }
        }

        private Dictionary<double, double> radius;

        public Dictionary<double, double> Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        private SortedDictionary<double, double> Vr;

        public SortedDictionary<double, double> VR
        {
            get { return Vr; }
            set { Vr = value; }
        }

        private SortedDictionary<double, double> dvdr;

        public SortedDictionary<double, double> dVdR
        {
            get { return dvdr; }
            set { dvdr = value; }
        }

        private SortedDictionary<double, double> vp; //V(p/p0)

        public SortedDictionary<double, double> Vp
        {
            get { return vp; }
            set { vp = value; }
        }

        #endregion

        #region phisics
        private string name_of_absorbat;

        public string Name_of_absorbat
        {
            get { return name_of_absorbat; }
            set { name_of_absorbat = value; }
        }

        private double p0pt, ONE_MINUS_p0pt, ptp0;

        public double Ptp0
        {
            get { return ptp0; }
            set { ptp0 = value; }
        }

        public double ONE_MINUS_P0Pt
        {
            get { return ONE_MINUS_p0pt; }
            set { ONE_MINUS_p0pt = value; }
        }

        public double P0pt
        {
            get { return p0pt; }
            set { p0pt = value; }
        }

        private double sigma, r, t, ro, vm;

        public double Vm
        {
            get { return vm; }
            set { vm = value; }
        }

        public double Ro
        {
            get { return ro; }
            set { ro = value; }
        }

        public double T
        {
            get { return t; }
            set { t = value; }
        }

        public double R
        {
            get { return r; }
            set { r = value; }
        }

        public double SIGMA
        {
            get { return sigma; }
            set { sigma = value; }
        }
        
        #endregion

        //other methods' consts
        double DRY_WEIGHT_OF_A_CELL = 0.02 /* B7 */, WEIGHT_OF_A_CELL, PART_OF_TIME_WHERE_FIND_W0 = 0.8;
        private double EPS = 0.00000001, MAX_PP0 = 0.996, END_PERCENT = 0.05, MAX_DVDR = 10;

        #region methods variables
        int falling_time;
        double max_v; //max rate

        public int Falling_time
        {
            get { return falling_time; }
            set { falling_time = value; }
        }

        int smooth_numb; // number of smoothing points (aming all points)

        public int Smooth_numb
        {
            get { return smooth_numb; }
            set { smooth_numb = value; }
        }

        int before_falling_numb;

        public int Before_falling_numb
        {
            get { return before_falling_numb; }
            set { before_falling_numb = value; }
        }

        int last_point_numb;

        public int Last_point_numb
        {
            get { return last_point_numb; }
            set { last_point_numb = value; }
        }

        double falling_value;

        public double Falling_value
        {
            get { return falling_value; }
            set { falling_value = value; }
        }

        double w0;

        public double W0
        {
            get { return w0; }
            set { w0 = value; }
        }

        #endregion

        //errors
        public bool OK = false;
        public string text = "", error_text = "";

        #endregion

        public double time;

        public Experiment_Data(string s)
        {
            path = s;
            Excel.Application ex;
            ex = new Excel.Application();
           // ex.Visible = false; 
           // ex.ScreenUpdating = false;
          //  ex.DisplayAlerts = false;
            ex.Workbooks.Open(path, Type.Missing, true, Type.Missing, Type.Missing, Type.Missing, true, Type.Missing, Type.Missing,                   Type.                            Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            if (ex != null)
            {
                try
                {
                    //  Excel.Sheets exSheets;
                    Excel.Worksheet exWorkSheet;
                    Excel.Range exCells;
                    //  exSheets = ex.Worksheets;
                    exWorkSheet = (Excel.Worksheet)ex.Worksheets.get_Item(1);
                    exCells = (Excel.Range)exWorkSheet.get_Range("A1", "C65536");
                    object[,] data;
                    data = (object[,])exCells.get_Value(Excel.XlRangeValueDataType.xlRangeValueDefault);
                    
                    int min = data.GetLowerBound(1), max = data.GetUpperBound(1);
                    double a = 0;
                    bool end_flag = false;
                    for (int R = data.GetLowerBound(0); R <= data.GetUpperBound(0) && !end_flag; R++)
                        for (int C = min; C <= max && !end_flag; C++)
                        {
                            if (C == min)
                            {
                                if (data.GetValue(R, C) != null)
                                    a = (double)data.GetValue(R, C);
                                else
                                    end_flag = true;
                            }
                            if (C == max)
                            {
                                values.Add((int)((double)data.GetValue(R, C)), a);
                            }
                        }
       //             cut(values);

                    exWorkSheet = (Excel.Worksheet)ex.Worksheets.get_Item(3);
                    exCells = (Excel.Range)exWorkSheet.get_Range("D11", Type.Missing);
                    name_of_absorbat = exCells.get_Offset(0, 0).Value2.ToString();
                    exCells = (Excel.Range)exWorkSheet.get_Range("B14", Type.Missing);
                    ro = (double)exCells.get_Offset(0, 0).Value2;
                    p0pt = (double)exCells.get_Offset(1, 0).Value2;
                    ONE_MINUS_p0pt = 1 - p0pt;
                    ptp0 = 1 / p0pt;
                    sigma = (double)exCells.get_Offset(2, 0).Value2;
                    vm = (double)exCells.get_Offset(3, 0).Value2;
                    r = (double)exCells.get_Offset(4, 0).Value2;
                    t = (double)exCells.get_Offset(5, 0).Value2;
                    DRY_WEIGHT_OF_A_CELL = (double)exCells.get_Offset(-7, 0).Value2;
                    WEIGHT_OF_A_CELL = (double)exCells.get_Offset(-8, 0).Value2;
                    WEIGHT_OF_A_CELL = DRY_WEIGHT_OF_A_CELL - WEIGHT_OF_A_CELL;
                    // WEIGHT_OF_A_CELL = 1;
                    //or 1

                    ex.Windows[1].Close(false, s, Type.Missing);
                    ex.Quit();
                    
                    OK = true;
                }
                catch (Exception exep)
                {
                    OK = false;
                    error_text = '\n' + exep.Message + '\n' + exep.Source;
                }
            }
        }

        int cut_numb = 300;

        void cut(SortedDictionary<int, double> v)
        {
            int t, n = v.Count, m = 0;
            if (n % 2 == 0)
                t = (n / 2) * 10;
            else
                t = ((n - 1) / 2) * 10;

            while (t < n * 10)
            {
                m = 0;
                while (m < cut_numb)
                {
                    if (v[t] == v[t + m * 10])
                        m++;
                    else
                        break;
                }
                if (m == cut_numb)
                    break;
                else
                    t += m * 10;
            }
            while (t < n * 10)
            {
                v.Remove(t);
                t += 10;
            }
        }

        #region methods
        public void sleek() //this algorithm looks then a falling begins and tooks previus values which are similar and average them
        {
            if (sleekmass == null)
                sleekmass = new SortedDictionary<int, double>(values);
            else
                sleekmass.Clear();
            double a, b, c, d;
            
            int i = 1;
            while(i + 3 < sleekmass.Count)
            {
                if (Math.Abs(sleekmass[10 * i] - sleekmass[10 * i + 10]) < EPS)
                {
                    a = sleekmass[10 * i + 20];
                    b = sleekmass[10 * i + 10];
                    c = sleekmass[10 * i];
                    if (Math.Abs(sleekmass[10 * i + 10] - sleekmass[10 * i + 20]) < EPS)
                    {
                        while (i + 3 < sleekmass.Count && Math.Abs(sleekmass[10 * i + 10] - sleekmass[10 * i + 20]) < EPS)
                        {
                            d = c;
                            c = b;
                            b = a;
                            a = sleekmass[10 * i + 30];
                            i++;
                        }
                        if (i + 3 < sleekmass.Count)
                        {
                            sleekmass[10 * i] = sleekmass[10 * i - 10] - (sleekmass[10 * i - 10] - sleekmass[10 * i + 20]) * 10 / 40;
                            sleekmass[10 * i + 10] = sleekmass[10 * i - 10] - (sleekmass[10 * i - 10] - sleekmass[10 * i + 20]) * 20 / 40;
                        }
                    }
                    else
                    {
                        sleekmass[10 * i + 10] = (sleekmass[10 * i] + sleekmass[10 * i + 20]) / 2;
                    }


                    i += 2;
                }
                else
                    i++;
            }

        }

        public void diff(int n)
        {
            int h;
            double a, b, c, d, e, v;
            rate = new SortedDictionary<int, double>();

            h = n * 10; //10 - time period between getting masses
            smooth_numb = n;
            a = b = c = d = e = 0;
            for (int j = 1; j <= n; j++)
                a += sleekmass[j * 10];
            a = a / n;

            for (int j = n + 1; j <= 2 * n; j++)
                b += sleekmass[j * 10];
            b = b / n;

            for (int j = 2 * n + 1; j <= 3 * n; j++)
                c += sleekmass[j * 10];
            c = c / n;

            for (int j = 3 * n + 1; j <= 4 * n; j++)
                d += sleekmass[j * 10];
            d = d / n;

            int i = 4 * n + 1;
            while (i + n < sleekmass.Count)
            {
                for (int j = 1; j <= n; j++)
                {
                    e += sleekmass[i * 10];
                    i++;
                }
                e = e / n;

                v = (-e + 8 * d - 8 * b + a) / (12 * h);
                rate.Add((i - 1) * 10 - 2 * h, -v * 60000 / (ro * WEIGHT_OF_A_CELL));

                a = b;
                b = c;
                c = d;
                d = e;
                e = 0;
            }
        }

        public void diff2(int n)
        {
            double h = 0.0001;
            int t1, t2, t3, t4, t5; //t4 is interested
            double v;

            if (rate == null)
                rate = new SortedDictionary<int, double>();
            else
                rate.Clear();

            // preparation
            int i = 10;
            t1 = i;

            while (Math.Abs(values[t1] - values[i]) < EPS) i += 10;
            t2 = i;

            while (Math.Abs(values[t2] - values[i]) < EPS) i += 10;
            t3 = i;

            while (Math.Abs(values[t3] - values[i]) < EPS) i += 10;
            t4 = i;

            while (Math.Abs(values[t4] - values[i]) < EPS) i += 10;
            t5 = i;

            //counting derivative
            while (i < values.Count * 10)
            {
                v = (12 * h) / (-t5 + 8 * t4 - 8 * t2 + t1);
                if (!rate.ContainsKey((int)((t1 + t2 + t3 + t4 + t5) / 5)))
                    rate.Add((int)((t1 + t2 + t3 + t4 + t5) / 5), v * 60000 / (ro * WEIGHT_OF_A_CELL));
                t1 = t2; t2 = t3; t3 = t4; t4 = t5;

                while (Math.Abs(values[t5] - values[i]) < EPS && i < values.Count * 10) i += 10;
                t5 = i;
            }
        }


        //public void diff2(int n)
        //{
        //    double h = 0.0001;
        //    int t1, t2, t3, t4, t5; //t4 is interested
        //    double v;

        //    if (rate == null)
        //        rate = new SortedDictionary<int, double>();
        //    else
        //        rate.Clear();

        //    // preparation
        //    int i = 10;
        //    t1 = i;

        //    while (Math.Abs(values[t1] - values[i]) < EPS) i += 10;
        //    t2 = i;

        //    while (Math.Abs(values[t2] - values[i]) < EPS) i += 10;
        //    t3 = i;

        //    while (Math.Abs(values[t3] - values[i]) < EPS) i += 10;
        //    t4 = i;

        //    while (Math.Abs(values[t4] - values[i]) < EPS) i += 10;
        //    t5 = i;

        //    //counting derivative
        //    while (i < values.Count * 10)
        //    {
        //        v = (12 * h) / (-t5 + 8 * t4 - 8 * t2 + t1);
        //        if(!rate.ContainsKey((int)((t1 + t2 + t3 + t4 + t5) / 5)))
        //            rate.Add((int)((t1 + t2 + t3 + t4 + t5) / 5), v * 60000 / (ro * WEIGHT_OF_A_CELL));
              
        //      //  else
        //            //rate.Add((int)((t1 + t2 + t3 + t4 + t5) / 5) + 10, -v * 60000 / (ro * WEIGHT_OF_A_CELL));
        //        t1 = t2; t2 = t3; t3 = t4; t4 = t5;

        //        while (Math.Abs(values[t5] - values[i]) < EPS && i < values.Count * 10) i += 10;
        //        t5 = i;
        //    }
        //}

        public double find_min_before_falling()
        {
            double min, s = 0, d;
            int j = 0, h = Smooth_numb * 10;
            max_v = 0;

            min = rate[((int)rate.Count / 10) * h - h]; s += min; j++;
            for(int i = rate.Count / 10; i <= rate.Count / 4; i++)
            {
                d = rate[i * h];
                s += d; j++;
                if (d > 0.6 * s / j)
                    if (d < min && d > 0) min = d;
                if (d > max_v && d < 1.6 * s / j) max_v = d;
            }

        	return min;
        }

        public void smooth2(int N, int n)
        {
            if (smoothedrate == null)
                smoothedrate = new SortedDictionary<int, double>();
            else
                smoothedrate.Clear();

            int i = 0;
            double s = 0;
            while (i + N < rate.Count)
            {
                for (int j = 0; j < N; j++)
                {
                    s += rate[rate.Keys.ElementAt(i)];
                    i++;
                }
                s = s / N;
                smoothedrate.Add(i * 10, s);
                s = 0;
            }


        }


        public int find_falling()
        {
            double av = find_min_before_falling();
        	double a, b, c, d, e;
            int i = rate.Count / 4, h;
            h = Smooth_numb * 10;
            a = rate[i * h]; i++;
            b = rate[i * h]; i++;
            c = rate[i * h]; i++;
            d = rate[i * h]; i++;
            e = rate[i * h]; i++;
            while (a > av || b > av || c > av || d > av || e > av)
            {
                a = b;
                b = c;
                c = d;
                d = e;
                i++;
                e = rate[i * h];
            }
            i -= 2; //return to a;
            while (rate[i * h] < av)
                i--;
            while (rate[i * h] < rate[(--i) * h]) ;
            falling_time = i * h;
            falling_value = rate[falling_time];

	        return falling_time;
        }

        // N - number of points before falling; n - number of points in the last before falling, einforcement
        public void smooth(int N, int n)
        {
            int h = Smooth_numb * 10, i = 3, j = 1, B = (int)(falling_time / (10 * Smooth_numb * N)); //i=3 casuse first element of rate s 3*h
            double s = rate[i * h], c = 0, d;
            before_falling_numb = N; last_point_numb = n;
            if (smoothedrate == null)
                smoothedrate = new SortedDictionary<int, double>();
            else
                smoothedrate.Clear();

	        do
	        {
		        if(j < B)
		        {
                    i++;
				    s += rate[i * h];
				    j++;
		        }
		        else
		        {
			        s /= B;
			        smoothedrate.Add(i * h, s);
		            s = 0;
			        j = 0;
		        }
	        }
	        while(smoothedrate.Count < N - 1);

	        //neighbourhood of falling
            B = B / n; j = 0;
            double r1 = 0, r2 = 0;
            int last_time = 0;
            do
            {
                if (j < B)
                {
                    i++;
                    if (i * h < falling_time)
                    {
                        s += rate[i * h];
                        j++;
                    }
                }
                else
                {
                    s /= B;
  //                  if(rate[i * h + h] > s)
                     //   smoothedrate.Add(i * h, (s+rate[i * h + h])/2);
      //              else
                    smoothedrate.Add(i * h, s);
                    if (r1 == 0) r1 = s;
                    else if (r2 == 0) { r2 = r1; r1 = s; }
                    else if (s > r1 && r2 > r1 || s < r1 && r2 < r1)
                        //if (s > smoothedrate[i * h - j*h] && smoothedrate[i * h - 2 *j* h] > smoothedrate[i * h - j*h])
                        smoothedrate[i * h - B * h] = (s + r2) / 2;

                    if (r1 != s)
                    { r2 = r1; r1 = s; }

                    s = 0;
                    j = 0;
                }
            }
            while (i * h < falling_time);
            if (j > 0)
            {
                s = s / j;
                smoothedrate.Add(i * h, s);
                r2 = r1; r1 = s; last_time = i * h;
            }


            //end part
            double e, f;
            bool last_point_flag = false;
            d = rate[i * h - h];
            i++;
            while (i < rate.Count && d > 0) //&& rate[i * h] > END_PERCENT * max_v
	        {
                c = rate[i * h];
                e = rate[i * h + h];
                f = rate[i * h + 2 * h];

                //if(c < d && c > 0 && e > d)
                //{
                //    smoothedrate.Add(i * h, (c + e) / 2);
                //    d = (c + e) / 2;
                //}
                //if (c < d && c > 0)
                //{
                //    smoothedrate.Add(i * h, c);
                //    d = c;
                //}
                //else if (c > 0 && c > d && (c + e) / 2 < d)
                //{
                //    smoothedrate.Add(i * h, (c + e) / 2);
                //    d = (c + e) / 2;
                //}
                //else if (c > 0 && c > d && e > d && f > d)
                //{
                //    smoothedrate.Add(i * h + 2 * h, (c + e + f) / 3);
                //    i += 2;
                //    d = (c + e + f) / 3;
                //}

                if ((c + e + rate[i * h - h] + f) / 4 < d && c > 0)
                {
                    d = (c + e + rate[i * h - h] + f) / 4;
                    if (d > 0)
                        smoothedrate.Add(i * h, d);
                
                    //if (!last_point_flag && (d > r1 && r2 > r1 || d < r1 && r2 < r1))
                    //{ last_point_flag = true; smoothedrate[last_time] = (d + r2) / 2; }
                }
                i++;
	        }
           // text += "   " + smoothedrate[i * h].ToString();
            //the end of the whole
	        s = c;
	        j = 1;
	        //B = B / 2;
	        while(i < rate.Count)
	        {
			    if(j < B)
		        {
		            s += rate[i * h];
			        j++;
		        }
		        else
		        {
			        s /= B;
			        smoothedrate.Add(i * h, s);
		            s = 0;
			        j = 0;
		        }
                i++;
	        }
        }

        private double find_w0()
        {
            int h = smooth_numb * 10;
	        double maxv = 0;

            foreach(KeyValuePair<int, double> kvp in smoothedrate)
            {
                if (kvp.Key > PART_OF_TIME_WHERE_FIND_W0 * falling_time && kvp.Key < falling_time)
                {
                    if (kvp.Value > maxv) maxv = kvp.Value;
                }
                else if(kvp.Key >= falling_time)
                    break;
            }

            return maxv;
        }

        public void radius_counting()
        {
            double C, pp0, r, log10 = Math.Log(10);
            if(w0 == null || w0 <= 0)
                w0 = find_w0();
            if (radius == null)
                radius = new Dictionary<double, double>();
            else
                radius.Clear();
            if (vp == null)
                vp = new SortedDictionary<double, double>();
            else
                vp.Clear();
            if (Vr == null)
                Vr = new SortedDictionary<double, double>();
            else
                Vr.Clear();

	        C = -2 * vm * sigma / (R * t);
            foreach (KeyValuePair<int, double> kvp in smoothedrate)
            {
                pp0 = ptp0 * (1 - Math.Pow(ONE_MINUS_p0pt, kvp.Value / w0));

                if (pp0 > MAX_PP0)
                    pp0 = MAX_PP0;

                if (!vp.ContainsKey((sleekmass[kvp.Key] - DRY_WEIGHT_OF_A_CELL) / (ro * WEIGHT_OF_A_CELL)))
                    vp.Add((sleekmass[kvp.Key] - DRY_WEIGHT_OF_A_CELL) / (ro * WEIGHT_OF_A_CELL), pp0);
                
                if (pp0 > 0)
                {
                    r = C / Math.Log(pp0);
                    r *= 100000000; //to ankstrem
              //      text += (Math.Log(r) / log10).ToString() + ' ';
                    if (!Vr.ContainsKey(r) && r > 0 && Math.Log(r) > 0 && Math.Log(r) / log10 < 4)
                        Vr.Add(r, (sleekmass[kvp.Key] - DRY_WEIGHT_OF_A_CELL) / (ro * WEIGHT_OF_A_CELL));

                    if (r > 0)
                    {
                        r = Math.Log(r) / log10;
                        if (!radius.ContainsKey((sleekmass[kvp.Key] - DRY_WEIGHT_OF_A_CELL) / (ro * WEIGHT_OF_A_CELL)))
                            radius.Add((sleekmass[kvp.Key] - DRY_WEIGHT_OF_A_CELL) / (ro * WEIGHT_OF_A_CELL), r);
                    }
                }
            }
            //foreach (KeyValuePair<double, double> kvp in Vr)
            //{
            //  //  text += (Math.Log(kvp.Key) / log10).ToString() + ' ';
            //}
        }

        public void dVdR_counting()
        {
            double h, r1 = 0, v1 = 0, v2, d, log10 = Math.Log(10);
            if (dvdr == null)
                dvdr = new SortedDictionary<double, double>();
            else
                dvdr.Clear();

            foreach (KeyValuePair<double, double> kvp in Vr)
            {
              //  text += kvp.Key.ToString() + "  " + kvp.Value.ToString() + '\n';
                if (r1 == 0)
                {
                 //   text += "a\n\n";
                    r1 = kvp.Key; v1 = kvp.Value;
                }
                else
                {
                    h = kvp.Key - r1;
                    v2 = kvp.Value;
                    d = (v2 - v1) / h;
                    if(Math.Abs(h) > 0.00000001 && d >= 0)
                   // if (d >= 0)
                    {
                        dvdr.Add(Math.Log(kvp.Key) / log10, d);
                    } 
                //    text += (Math.Log(kvp.Key) / log10).ToString() + "    " + d.ToString() + '\n';
                    r1 = kvp.Key;
                    v1 = v2;
                }
            }

        }

        public void find_w0_with_vmax(double vmax)
        {
            double mmax = (vmax * ro * WEIGHT_OF_A_CELL) + DRY_WEIGHT_OF_A_CELL;
            int maxtime;
            if (mmax < sleekmass[3 * Smooth_numb * 10])
            {
                int i = 3;
                int h = Smooth_numb * 10;
                while (mmax < sleekmass[h * i])
                    i++;
                maxtime = h * i;
                w0 = rate[maxtime];
            }
        }

        #endregion

        const string rate_str = " Rate(Time)", radius_str = " V(Log(Radius))", dvdr_str = " dVdr(Log(Radius))", vpp0_str = " V(PP0)";

        public void Save(string s)
        {
            string srate, sradius, sdvdr, svpp0;
            string[] ss = new string[2]; 
            ss = s.Split('.');
            srate = ss[0] + rate_str + '.' + ss[1];
            sradius = ss[0] + radius_str + '.' + ss[1];
            sdvdr = ss[0] + dvdr_str + '.' + ss[1];
            svpp0 = ss[0] + vpp0_str + '.' + ss[1];

            StreamWriter sw1 = new StreamWriter(srate, false, Encoding.Unicode);
            foreach (KeyValuePair<int, double> kvp in SmoothedRate)
            {
                sw1.Write(kvp.Key); sw1.Write("\t"); sw1.WriteLine(kvp.Value);
            }
            sw1.Close();


            StreamWriter sw2 = new StreamWriter(sradius, false, Encoding.Unicode);
            foreach (KeyValuePair<double, double> kvp in Radius)
            {
                sw2.Write(kvp.Key); sw2.Write("\t"); sw2.WriteLine(kvp.Value);
            }
            sw2.Close();


            StreamWriter sw3 = new StreamWriter(sdvdr, false, Encoding.Unicode);
            foreach (KeyValuePair<double, double> kvp in dVdR)
            {
                sw3.Write(kvp.Key); sw3.Write("\t"); sw3.WriteLine(kvp.Value);
            }
            sw3.Close();

            StreamWriter sw4 = new StreamWriter(svpp0, false, Encoding.Unicode);
            foreach (KeyValuePair<double, double> kvp in Vp)
            {
                sw4.Write(kvp.Key); sw4.Write("\t"); sw4.WriteLine(kvp.Value);
            }
            sw4.Close();

        }
    }
}
