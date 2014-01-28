using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Threading;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;

namespace Dynamic_Desorption_Porometry_Method
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Page1 : Page
    {
        const int exp_numb = 4;
        private string[] path = new string[exp_numb];
        static public Data.Experiment_Data[] experiment = new Data.Experiment_Data[exp_numb];
        static public int numb = 0; //number of experiments
        public static readonly DependencyProperty MyDPProperty;
        static private bool original_mass = true, sleeked_mass = true;
        private LineGraph[] originalmassGraph = new LineGraph[exp_numb];
        private LineGraph[] sleekedmassGraph = new LineGraph[exp_numb];
        private Pen[] p1 = new Pen[4];
        private Pen[] p2 = new Pen[4];
        private int cur_exp = 0;

        private EnumerableDataSource<double>[] yDataSource = new EnumerableDataSource<double>[4];
        private EnumerableDataSource<int>[] xDataSource = new EnumerableDataSource<int>[4];
        private EnumerableDataSource<double>[] yDataSource2 = new EnumerableDataSource<double>[4];
        private EnumerableDataSource<int>[] xDataSource2 = new EnumerableDataSource<int>[4];

        public Page1()
        {
            InitializeComponent();

            p1[0] = new Pen(Brushes.LightSteelBlue, 2);
            p1[1] = new Pen(Brushes.Silver, 2);
            p1[2] = new Pen(Brushes.DarkGreen, 2);
            p1[3] = new Pen(Brushes.DarkRed, 2);

            p2[0] = new Pen(Brushes.MidnightBlue, 2);
            p2[1] = new Pen(Brushes.Goldenrod, 2);
            p2[2] = new Pen(Brushes.Green, 2);
            p2[3] = new Pen(Brushes.Red, 2);
        }

        static Page1()
        {
            FrameworkPropertyMetadata fpm = new FrameworkPropertyMetadata();
            fpm.Journal = true;
        }

        private void MenuItem1_Click(object sender, RoutedEventArgs e)
        {
            if (numb < exp_numb)
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = ".xls|*.xls";
                openFileDialog1.ShowDialog();
                path[numb] = openFileDialog1.FileName;
                Open_Item.IsEnabled = false;
                if (path[numb] != "")
                {
                    Thread t = new Thread(delegate()
                    {
                        progressBar1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                        (ThreadStart)delegate() { progressBar1.IsIndeterminate = true; });
                        label7.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                            (ThreadStart)delegate() { label7.FontSize = 13; label7.Content = "Loading data from file.. "; });

                        experiment[numb] = new Data.Experiment_Data(path[numb]);
                        progressBar1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                        (ThreadStart)delegate() { progressBar1.IsIndeterminate = false; });
                        label7.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                        (ThreadStart)delegate() { label7.FontSize = 10; label7.Content = "Data path: " + experiment[numb].Path; });

                        if (experiment[numb] != null && experiment[numb].OK)
                        {
                            var yDataSource = new EnumerableDataSource<double>(experiment[numb].Values.Values);
                            yDataSource.SetYMapping(Y => Y);

                            var xDataSource = new EnumerableDataSource<int>(experiment[numb].Values.Keys);
                            xDataSource.SetXMapping(X => X / 60);

                            CompositeDataSource compositeDataSource = new CompositeDataSource(xDataSource, yDataSource);
                            plotter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate()
                                {
                                    originalmassGraph[numb] = plotter.AddLineGraph(compositeDataSource,
                                    p1[numb],
                                    new PenDescription("Mass(Time) " + (numb + 1).ToString()));
                                    originalmassGraph[numb].Visibility = original_mass ? Visibility.Visible : Visibility.Hidden;
                                });
                            plotter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                 (ThreadStart)delegate() { plotter.Viewport.FitToView(); });


                            experiment[numb].sleek();
                            var yDataSource2 = new EnumerableDataSource<double>(experiment[numb].SleekMass.Values);
                            yDataSource2.SetYMapping(Y => Y);
                            var xDataSource2 = new EnumerableDataSource<int>(experiment[numb].Values.Keys);
                            xDataSource2.SetXMapping(X => X / 60);

                            CompositeDataSource compositeDataSource2 = new CompositeDataSource(xDataSource2, yDataSource2);
                            plotter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,
                                (ThreadStart)delegate()
                                {
                                    sleekedmassGraph[numb] = plotter.AddLineGraph(compositeDataSource2,
                                        p2[numb],
                                        new PenDescription("Sleek Mass (Time) " + (numb + 1).ToString()));
                                    sleekedmassGraph[numb].Visibility = sleeked_mass ? Visibility.Visible : Visibility.Hidden;
                                    plotter.Viewport.FitToView();
                                    Open_Item.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                        (ThreadStart)delegate() { Open_Item.IsEnabled = true; });
                                });

                            // MessageBox.Show(experiment[numb].time.ToString());
                            Thread.Sleep(60 * 10);

                            cur_exp = numb;
                            experiment[numb].diff(10);
                            experiment[numb].find_falling();
                            experiment[numb].smooth(10, 5);
                            experiment[numb].radius_counting();
                            experiment[numb].dVdR_counting();
                            //  MessageBox.Show(experiment[numb].text);
                            numb++;


                            textBox1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate() { textBox1.Text = experiment[cur_exp].Name_of_absorbat; });
                            textBox2.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate() { textBox2.Text = experiment[cur_exp].T.ToString(); });
                            textBox3.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate() { textBox3.Text = experiment[cur_exp].Ro.ToString(); });
                            textBox4.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate() { textBox4.Text = experiment[cur_exp].P0pt.ToString(); });
                            textBox5.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate() { textBox5.Text = experiment[cur_exp].SIGMA.ToString(); });
                            textBox6.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate() { textBox6.Text = experiment[cur_exp].Vm.ToString(); });
                            textBox7.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate() { textBox7.Text = numb.ToString(); });
                        }
                        else
                        {
                            MessageBox.Show("Wrong file format!\n" + path[numb] + '\n' + experiment[numb].error_text);
                            Open_Item.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                (ThreadStart)delegate() { Open_Item.IsEnabled = true; });
                        }
                    });
                    t.Start();
                }
                else { Open_Item.IsEnabled = true; }
            }
            else
            {
                MessageBox.Show("There maximum can be 4 experiments!");
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            checkBox1.IsChecked = original_mass;
            checkBox2.IsChecked = sleeked_mass;

            for (int i = 0; i < numb; i++)
            {
                if (experiment[i] != null && experiment[i].OK)
                {
                    if (i == numb - 1)
                    {
                        label7.FontSize = 10; label7.Content = "Data path: " + experiment[i].Path;
                    }

                    yDataSource[i] = new EnumerableDataSource<double>(experiment[i].Values.Values);
                    yDataSource[i].SetYMapping(Y => Y);
                    xDataSource[i] = new EnumerableDataSource<int>(experiment[i].Values.Keys);
                    xDataSource[i].SetXMapping(X => X / 60);

                    CompositeDataSource compositeDataSource = new CompositeDataSource(xDataSource[i], yDataSource[i]);
                    originalmassGraph[i] = plotter.AddLineGraph(compositeDataSource,
                    p1[i],
                    new CircleElementPointMarker
                    {
                        //Size = 4,
                        //Brush = p2[i].Brush,
                        Size = 4,
                        Brush = p1[i].Brush,
                        Fill = Brushes.Orange
                    },
                    new PenDescription("Mass(Time) " + (i + 1).ToString())).LineGraph;

                    originalmassGraph[i].Visibility = original_mass ? Visibility.Visible : Visibility.Hidden;
                    plotter.Viewport.FitToView();

                    textBox1.Text = experiment[i].Name_of_absorbat;
                    textBox2.Text = experiment[i].T.ToString();
                    textBox3.Text = experiment[i].Ro.ToString();
                    textBox4.Text = experiment[i].P0pt.ToString();
                    textBox5.Text = experiment[i].SIGMA.ToString();
                    textBox6.Text = experiment[i].Vm.ToString();
                    textBox7.Text = (i + 1).ToString();

                    yDataSource2[i] = new EnumerableDataSource<double>(experiment[i].SleekMass.Values);
                    yDataSource2[i].SetYMapping(Y => Y);
                    xDataSource2[i] = new EnumerableDataSource<int>(experiment[i].Values.Keys);
                    xDataSource2[i].SetXMapping(X => X / 60);

                    CompositeDataSource compositeDataSource2 = new CompositeDataSource(xDataSource2[i], yDataSource2[i]);
                    sleekedmassGraph[i] = plotter.AddLineGraph(compositeDataSource2,
                    p2[i],
                    new PenDescription("Sleek Mass (Time) " + (i + 1).ToString()));
                    sleekedmassGraph[i].Visibility = sleeked_mass ? Visibility.Visible : Visibility.Hidden;
                    plotter.Viewport.FitToView();
                }
            }
        }

        private void checkBox1_Click(object sender, RoutedEventArgs e)
        {
            original_mass = !original_mass;
            for (int i = 0; i < numb; i++)
            {
                if (originalmassGraph[i] != null)
                {
                    originalmassGraph[i].Visibility = original_mass ? Visibility.Visible : Visibility.Hidden;
                    plotter.FitToView();
                }
            }
        }

        private void checkBox2_Click(object sender, RoutedEventArgs e)
        {
            sleeked_mass = !sleeked_mass;
            for (int i = 0; i < numb; i++)
            {
                if (sleekedmassGraph[i] != null)
                {
                    sleekedmassGraph[i].Visibility = sleeked_mass ? Visibility.Visible : Visibility.Hidden;
                    plotter.FitToView();
                }
            }
        }

        private void textBox7_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (textBox7.Text != "")
                {
                    int i = Convert.ToInt32(textBox7.Text);
                    if (i > 0 && i <= numb)
                    {
                        textBox1.Text = experiment[i - 1].Name_of_absorbat;
                        textBox2.Text = experiment[i - 1].T.ToString();
                        textBox3.Text = experiment[i - 1].Ro.ToString();
                        textBox4.Text = experiment[i - 1].P0pt.ToString();
                        textBox5.Text = experiment[i - 1].SIGMA.ToString();
                        textBox6.Text = experiment[i - 1].Vm.ToString();
                        label7.FontSize = 10; label7.Content = "Data path: " + experiment[i - 1].Path;
                        cur_exp = i - 1;
                    }
                }
            }
            catch { }
        }

        private void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            string ss;
            SaveFileDialog savefiledialog1 = new SaveFileDialog();
            savefiledialog1.Filter = ".txt|*.txt";
            savefiledialog1.ShowDialog();
            ss = savefiledialog1.FileName;
            if (ss.Length != 0 && experiment[cur_exp] != null)
                experiment[cur_exp].Save(ss);
            else if (experiment[cur_exp] == null)
                MessageBox.Show("There is no experiments");
            else
                MessageBox.Show("You didn't enter any text!");
            //     MessageBox.Show(cur_exp.ToString());
        }


        double[] water_ro = new double[3] { 0, 1, 2 }, water_ppo = new double[3] { 0, 1, 2 };
        double[] water_vm = new double[3] { 0, 1, 2 }, water_sigma = new double[3] { 0, 1, 2 };

        double[] gexan_ro = new double[3] { 0.6503, 0.6433, 0.6412 }, gexan_ppo = new double[3] { 0.251182777, 0.308158738, 0.375146388 };
        double[] gexan_vm = new double[3] { 132.5, 133.96550, 135.431 }, gexan_sigma = new double[3] { 0.00000174, 0.00000168492, 0.000001631 };

        double[] ciclogexan_ro = new double[3] { 0.76915, 0.764375, 0.7596 }, ciclogexan_ppo = new double[3] { 0.163363243, 0.202044057, 0.247871592 };
        double[] ciclogexan_vm = new double[3] { 109.419489, 110.10732215, 110.7951553 }, ciclogexan_sigma = new double[3] { 0.000002375, 0.00000231, 0.000002245 };

        double[] benzol_ro = new double[3] { 0.86500, 0.8618, 0.8576 }, benzol_ppo = new double[3] { 0.160174512, 0.199049055, 0.237923598 };
        double[] benzol_vm = new double[3] { 89.93, 90.63587839, 91.07975746 }, benzol_sigma = new double[3] { 0.00000275, 0.0000026815, 0.000002613 };

        private void textBox2_TextChanged(object sender, TextChangedEventArgs e) //temp changed
        {
            if (textBox1.Text != "")
            {
                double t = 303;
                try
                {
                    t = Convert.ToDouble(textBox2.Text);
                    if (t >= 303 && t <= 313)
                    {
                        if (textBox1.Text == "Циклогексан")
                        {
                            if (t >= 303 && t <= 308 && experiment[cur_exp].SmoothedRate != null)
                            {
                                experiment[cur_exp].Ro = ciclogexan_ro[0] + (t - 303) * (ciclogexan_ro[1] - ciclogexan_ro[0]) / 5;
                                experiment[cur_exp].P0pt = ciclogexan_ppo[0] + (t - 303) * (ciclogexan_ppo[1] - ciclogexan_ppo[0]) / 5;
                                experiment[cur_exp].Ptp0 = 1 / experiment[cur_exp].P0pt;
                                experiment[cur_exp].ONE_MINUS_P0Pt = 1 - experiment[cur_exp].P0pt;
                                experiment[cur_exp].SIGMA = ciclogexan_sigma[0] + (t - 303) * (ciclogexan_sigma[1] - ciclogexan_sigma[0]) / 5;
                                experiment[cur_exp].Vm = ciclogexan_vm[0] + (t - 303) * (ciclogexan_vm[1] - ciclogexan_vm[0]) / 5;
                                experiment[cur_exp].T = t;
                            }
                            else
                            {
                                experiment[cur_exp].Ro = ciclogexan_ro[1] + (t - 308) * (ciclogexan_ro[2] - ciclogexan_ro[1]) / 5;
                                experiment[cur_exp].P0pt = ciclogexan_ppo[1] + (t - 308) * (ciclogexan_ppo[2] - ciclogexan_ppo[1]) / 5;
                                experiment[cur_exp].Ptp0 = 1 / experiment[cur_exp].P0pt;
                                experiment[cur_exp].ONE_MINUS_P0Pt = 1 - experiment[cur_exp].P0pt;
                                experiment[cur_exp].SIGMA = ciclogexan_sigma[1] + (t - 308) * (ciclogexan_sigma[2] - ciclogexan_sigma[1]) / 5;
                                experiment[cur_exp].Vm = ciclogexan_vm[1] + (t - 308) * (ciclogexan_vm[2] - ciclogexan_vm[1]) / 5;
                                experiment[cur_exp].T = t;
                            }

                        }
                        else if (textBox1.Text == "Бензол")
                        {
                            if (t >= 303 && t <= 308 && experiment[cur_exp].SmoothedRate != null)
                            {
                                experiment[cur_exp].Ro = benzol_ro[0] + (t - 303) * (benzol_ro[1] - benzol_ro[0]) / 5;
                                experiment[cur_exp].P0pt = benzol_ppo[0] + (t - 303) * (benzol_ppo[1] - benzol_ppo[0]) / 5;
                                experiment[cur_exp].Ptp0 = 1 / experiment[cur_exp].P0pt;
                                experiment[cur_exp].ONE_MINUS_P0Pt = 1 - experiment[cur_exp].P0pt;
                                experiment[cur_exp].SIGMA = benzol_sigma[0] + (t - 303) * (benzol_sigma[1] - benzol_sigma[0]) / 5;
                                experiment[cur_exp].Vm = benzol_vm[0] + (t - 303) * (benzol_vm[1] - benzol_vm[0]) / 5;
                                experiment[cur_exp].T = t;
                            }
                            else
                            {
                                experiment[cur_exp].Ro = benzol_ro[1] + (t - 308) * (benzol_ro[2] - benzol_ro[1]) / 5;
                                experiment[cur_exp].P0pt = benzol_ppo[1] + (t - 308) * (benzol_ppo[2] - benzol_ppo[1]) / 5;
                                experiment[cur_exp].Ptp0 = 1 / experiment[cur_exp].P0pt;
                                experiment[cur_exp].ONE_MINUS_P0Pt = 1 - experiment[cur_exp].P0pt;
                                experiment[cur_exp].SIGMA = benzol_sigma[1] + (t - 308) * (benzol_sigma[2] - benzol_sigma[1]) / 5;
                                experiment[cur_exp].Vm = benzol_vm[1] + (t - 308) * (benzol_vm[2] - benzol_vm[1]) / 5;
                                experiment[cur_exp].T = t;
                            }
                        }
                        //else if (textBox1.Text == "Вода")
                        //{
                        //    if (t >= 303 && t <= 308 && experiment[cur_exp].SmoothedRate != null)
                        //    {
                        //        experiment[cur_exp].Ro = water_ro[0] + (t - 303) * (water_ro[1] - water_ro[0]) / 5;
                        //        experiment[cur_exp].P0pt = water_ppo[0] + (t - 303) * (water_ppo[1] - water_ppo[0]) / 5;
                        //        experiment[cur_exp].Ptp0 = 1 / experiment[cur_exp].P0pt;
                        //        experiment[cur_exp].ONE_MINUS_P0Pt = 1 - experiment[cur_exp].P0pt;
                        //        experiment[cur_exp].SIGMA = water_sigma[0] + (t - 303) * (water_sigma[1] - water_sigma[0]) / 5;
                        //        experiment[cur_exp].Vm = water_vm[0] + (t - 303) * (water_vm[1] - water_vm[0]) / 5;
                        //        experiment[cur_exp].T = t;
                        //    }
                        //    else
                        //    {
                        //        experiment[cur_exp].Ro = water_ro[1] + (t - 308) * (water_ro[2] - water_ro[1]) / 5;
                        //        experiment[cur_exp].P0pt = water_ppo[1] + (t - 308) * (water_ppo[2] - water_ppo[1]) / 5;
                        //        experiment[cur_exp].Ptp0 = 1 / experiment[cur_exp].P0pt;
                        //        experiment[cur_exp].ONE_MINUS_P0Pt = 1 - experiment[cur_exp].P0pt;
                        //        experiment[cur_exp].SIGMA = water_sigma[1] + (t - 308) * (water_sigma[2] - water_sigma[1]) / 5;
                        //        experiment[cur_exp].Vm = water_vm[1] + (t - 308) * (water_vm[2] - water_vm[1]) / 5;
                        //        experiment[cur_exp].T = t;
                        //    }
                        //}
                        else if (textBox1.Text == "Гексан")
                        {
                            if (t >= 303 && t <= 308 && experiment[cur_exp].SmoothedRate != null)
                            {
                                experiment[cur_exp].Ro = gexan_ro[0] + (t - 303) * (gexan_ro[1] - gexan_ro[0]) / 5;
                                experiment[cur_exp].P0pt = gexan_ppo[0] + (t - 303) * (gexan_ppo[1] - gexan_ppo[0]) / 5;
                                experiment[cur_exp].Ptp0 = 1 / experiment[cur_exp].P0pt;
                                experiment[cur_exp].ONE_MINUS_P0Pt = 1 - experiment[cur_exp].P0pt;
                                experiment[cur_exp].SIGMA = gexan_sigma[0] + (t - 303) * (gexan_sigma[1] - gexan_sigma[0]) / 5;
                                experiment[cur_exp].Vm = gexan_vm[0] + (t - 303) * (gexan_vm[1] - gexan_vm[0]) / 5;
                                experiment[cur_exp].T = t;
                            }
                            else
                            {
                                experiment[cur_exp].Ro = gexan_ro[1] + (t - 308) * (gexan_ro[2] - gexan_ro[1]) / 5;
                                experiment[cur_exp].P0pt = gexan_ppo[1] + (t - 308) * (gexan_ppo[2] - gexan_ppo[1]) / 5;
                                experiment[cur_exp].Ptp0 = 1 / experiment[cur_exp].P0pt;
                                experiment[cur_exp].ONE_MINUS_P0Pt = 1 - experiment[cur_exp].P0pt;
                                experiment[cur_exp].SIGMA = gexan_sigma[1] + (t - 308) * (gexan_sigma[2] - gexan_sigma[1]) / 5;
                                experiment[cur_exp].Vm = gexan_vm[1] + (t - 308) * (gexan_vm[2] - gexan_vm[1]) / 5;
                                experiment[cur_exp].T = t;
                            }
                        }

                        experiment[cur_exp].diff(experiment[cur_exp].Smooth_numb);
                        experiment[cur_exp].find_falling();
                        experiment[cur_exp].smooth(experiment[cur_exp].Before_falling_numb,
                           experiment[cur_exp].Last_point_numb);
                        experiment[cur_exp].radius_counting();
                        experiment[cur_exp].dVdR_counting();

                        textBox2.Text = experiment[cur_exp].T.ToString();
                        textBox3.Text = experiment[cur_exp].Ro.ToString();
                        textBox4.Text = experiment[cur_exp].P0pt.ToString();
                        textBox5.Text = experiment[cur_exp].SIGMA.ToString();
                        textBox6.Text = experiment[cur_exp].Vm.ToString();

                    }
                }
                catch { }
            }
        }
    }
}
 