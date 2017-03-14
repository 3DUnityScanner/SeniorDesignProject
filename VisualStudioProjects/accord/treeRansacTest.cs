using System;
using System.Collections.Generic;
using Accord.Statistics.Models.Regression.Linear;
using Accord.MachineLearning;
using Accord.Math;
using Accord.MachineLearning.DecisionTrees;
using Accord.Statistics.Analysis;
using Accord.Math.Optimization.Losses;
using static accord.PositTest;
using static accord.icp;

/** Testing for the basic structures to be used in the cv algorithm **/

namespace treeRansacTest
{
    class Program
    {
        /**
        RANSAC notes: make 2d bounding box full-size of img, 
            **/

        //RANSAC func for dummy data 2D array
        private double[] ransac(double[][] data)
        {
            if (data == null)
                return null;
            //get column data from the first and second cols
            double[] col0 = new double[data.Length];
            double[] col1 = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                col0[i] = data[i][0];
                col1[i] = data[i][1];
            }

            //Ordinary Least Squares for learning
            OrdinaryLeastSquares ols = new OrdinaryLeastSquares();
            //Estimate a line through (col0,col1)
            SimpleLinearRegression regression = ols.Learn(col0, col1);
            //Predicted regression for original input (used for comparison)
            double[] predict = regression.Transform(col0);

            //fit linear regression with RANSAC
            //default sample parameters
            int trials = 1000;
            int samples = 20;
            double probability = 0.950;
            double errorThresh = 1.0;

            //RANSAC creation
            var ransac = new RANSAC<SimpleLinearRegression>(samples)
            {
                Probability = probability,
                Threshold = errorThresh,
                MaxEvaluations = trials,

                //The fitting function
                Fitting = delegate(int[] sample) 
                {
                    //training data
                    double[] inputs = col0.Get(sample);
                    double[] outputs = col0.Get(sample);

                    //Simple Linear Regression Model
                    return new OrdinaryLeastSquares().Learn(inputs, outputs);
                },
                //Define degenerate samples
                Degenerate = delegate(int[] sample) 
                {
                    //no check in the sample
                    return false;
                },
                //Define inliers
                Distances = delegate(SimpleLinearRegression r, double thresh)
                {
                    List<int> inliers = new List<int>();
                    for (int i = 0; i < col0.Length; i++)
                    {
                        //Error for every data point
                        double error = r.Transform(col0[i]) - col1[i];
                        //Squared Error check
                        if (error * error < thresh)
                            inliers.Add(i);
                    }
                    return inliers.ToArray();
                },

            };

            //Now compute another model using the RANSAC results
            int[] inlierList;
            SimpleLinearRegression robust = ransac.Compute(data.Length, out inlierList);

            if (robust == null)
                return null;

            //compute output of fitted model
            double[] output = robust.Transform(col0);

            //print
            Console.WriteLine("RANSAC inliers: ");
            for (int j = 0; j < output.Length; j++)
            {
                Console.WriteLine(col0[j] +","+ output[j]);
            }
            Console.ReadLine();

            return output;
        }

/**END RANSAC, BEGIN DECISION TREE**/
        
        //Global forest
        RandomForest forest;

        //doubles for probs and ints for predictions
        public void initTree(double[][] inputs, int[] outputs)
        {
            DecisionVariable[] vars =
            {
                new DecisionVariable("x", DecisionVariableKind.Continuous), //will use discrete values for pixels
                new DecisionVariable("y", DecisionVariableKind.Continuous),
            };

            //C4.5 Learning alg, for ind. trees
            //var c45 = new C45Learning(vars);
            //DecisionTree[] trees = forest.Trees;

            var alg = new RandomForestLearning(vars)
            {
                NumberOfTrees = 3,
                //CoverageRatio = 0.9,
                //Join = 50,
                SampleRatio = 1.0,
            };

            //TODO: Fix Learning
            forest = alg.Learn(inputs,outputs);
        }

        //Tests forest
        private void testTree(double[][] inputs, int[] expected)
        {
            if (forest == null)
                return;

            //compute forest outputs (simplified)
            int[] results = forest.Decide(inputs);

            //error
            double err = new ZeroOneLoss(expected).Loss(forest.Decide(inputs));
            
            //compute forest outputs
            //for (int i = 0; i < inputs.Length; i++)
            //{ 
            //        results[i] = forest.Decide(inputs[i]);                
            //}

            //confusion matrix to use as comparison data (performance metric)
            ConfusionMatrix confusionMatrix = new ConfusionMatrix(results, expected, 1, 0);
            Console.WriteLine("Error: " + err);
            Console.WriteLine("TPos: "+confusionMatrix.TruePositives);
            Console.WriteLine("FPos: " + confusionMatrix.FalsePositives);
            Console.WriteLine("TNeg: " + confusionMatrix.TrueNegatives);
            Console.WriteLine("FNeg: " + confusionMatrix.FalseNegatives);
            Console.WriteLine("Sens: " + confusionMatrix.Sensitivity);
            Console.WriteLine("Spec: " + confusionMatrix.Specificity);
            Console.WriteLine("Eff: " + confusionMatrix.Efficiency);
            Console.WriteLine("Acc: " + confusionMatrix.Accuracy);
            Console.ReadLine();
        }

        //fucntion to read a csv TODO

        static void Main(string[] args)
        {
            //TEST FOR TREE
            Program prog = new Program();
            //test data arrays, sorry for the copy/paste, it was just easier
            double[,] inputs = new double[,] {
            {-0.876847427782256,    1.99631882419913},
            {-0.748759324869685,    1.99724851416656},
            {-0.63557469450121, 1.97804657885838},
            {-0.513769071062192,    1.97322477728966},
            {-0.382577547284093,    1.95507722390739},
            {-0.275144211134104,    1.9238137888256},
            {-0.156802752494605,    1.94921969451005},
            {-0.0460020585481831,   1.89534254245404},
            {0.0841522573629316,    1.87310408178925},
            {0.192063131363708, 1.86815753225309},
            {0.238547032360424, 1.81166416536336},
            {0.381412694469276, 1.83086992461685},
            {0.431182118837231, 1.75531247939342},
            {0.562589082378018, 1.72544480585522},
            {0.553294268828727, 1.68904788552912},
            {0.730976261421586, 1.61052206353837},
            {0.722164981441283, 1.63311295208607},
            {0.861069302278025, 1.56245019650894},
            {0.82510794508344,  1.43584622512398},
            {0.82526113243736,  1.45639119641546},
            {0.94872162601077,  1.39336755213731},
            {1.0017052775839,   1.27576844658262},
            {0.966788666823623, 1.32137523307275},
            {1.03082894399269,  1.2284370225454},
            {1.08319563558295,  1.1430115893454},
            {0.920876421758384, 1.0378543880619},
            {0.994518276547599, 1.06497102256707},
            {0.954169422387943, 0.938084211116584},
            {0.90358608314434,  0.985255340596275},
            {0.877869854324478, 0.72914352468562},
            {0.866594018329422, 0.750257339720458},
            {0.75727838893834,  0.638917822242892},
            {0.65548951503697,  0.670717406446326},
            {0.687639625917881, 0.511655563034953},
            {0.656365077946712, 0.63854234588037},
            {0.491775914043228, 0.401874801595369},
            {0.355044890344968, 0.389639670058464},
            {0.275616567913215, 0.182958126126517},
            {0.338471037093842, 0.102347681685709},
            {0.10391809494267,  0.15296096122637},
            {0.238473941423481, -0.0708999650081961},
            {-0.00657753951083318,  0.16810793103585},
            {-0.09130705769294, -0.0321743990949914},
            {-0.29077203355174, -0.345025688767063},
            {-0.287555253061997,    -0.397984322825927},
            {-0.363424617632838,    -0.365636807709192},
            {-0.54407169101181, -0.512970643761903},
            {-0.709896799715478,    -0.546549210418757},
            {-1.00785721628696, -0.811837224369838},
            {-0.93278712214462, -0.687973275892451},
            {-0.123987649007882,    -1.54797648263711},
            {-0.247236701465217,    -1.54662946135943},
            {-0.369357682047641,    -1.53396875518735},
            {-0.497892177999878,    -1.52559795179504},
            {-0.606998699277538,    -1.51838622885357},
            {-0.751556975707837,    -1.46427031963956},
            {-0.85884861881095, -1.46414228917007},
            {-0.957834238130025,    -1.45416588803371},
            {-1.06160269832509, -1.4447832157624},
            {-1.16963434292188, -1.42603350749434},
            {-1.27211589537403, -1.4086788168053},
            {-1.38038329274706, -1.34565144246206},
            {-1.48086657388699, -1.27995520246713},
            {-1.54892766410633, -1.22326254133315},
            {-1.59788681937383, -1.22711593580737},
            {-1.68671149660754, -1.14189827582877},
            {-1.81268905111533, -1.14805053009159},
            {-1.80984133637657, -1.08334760169182},
            {-1.93885071072642, -1.01972374176963},
            {-1.97455267932884, -0.970515421605032},
            {-1.95318435886883, -0.883631209948009},
            {-1.98749965029243, -0.861879771987957},
            {-2.0421555395652,  -0.797813815081594},
            {-1.98418573420594, -0.82698683467539},
            {-2.06330760479235, -0.749495212625776},
            {-1.9642741340614,  -0.653639779185205},
            {-2.02025815525305, -0.53043161525989},
            {-1.94608199606774, -0.514425683121564},
            {-1.93435600631304, -0.435380422937908},
            {-1.82701765752586, -0.425058004329038},
            {-1.78838588881305, -0.312443513353481},
            {-1.80087403345128, -0.237312968599446},
            {-1.78422512579098, 0.013987951279965},
            {-1.68282832057107, -0.0639114652245816},
            {-1.75404247110007, -0.0755206534369962},
            {-1.56807330023599, 0.110795035930537},
            {-1.43833326811738, 0.170230560810247},
            {-1.35661466140466, 0.163613840797986},
            {-1.33636239703231, 0.334537756021892},
            {-1.29667760727799, 0.316006907420459},
            {-1.10990885746601, 0.474036646305678},
            {-0.845929173746086,    0.485303883803993},
            {-0.855794710826973,    0.395603117655894},
            {-0.684792549809302,    0.671166244919161},
            {-0.514222251521849,    0.652065554246795},
            {-0.387612556799943,    0.700858902286844},
            {-0.51939719025496, 1.02573533504353},
            {-0.228760024694072,    0.934903140383779},
            {-0.293782477244119,    1.00886167797763},
            {0.0134310122427387,    1.08202152527043}
            };
            double[][] jaggedInputs = inputs.ToJagged();
            int[] outputs = new int[] { 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
            double[,] ransacIn = new double[,]
            {
                {1, 0.42000000000000004},
                {3, 2.42},
                {5, 5.22},
                {7, 7.33},
                {9, 8.99},
                {11, 10.83},
                {13, 13.9},
                {15, 14.69},
                {17, 16.73},
                {1.2,   0.21999999999999997},
                {1.5,   2.3},
                {1.8,   1.3900000000000001},
                {2.1,   2.7},
                {2.4,   1.98},
                {2.7,   2.33},
                {3, 2.07},
                {3.3,   3.7199999999999998},
                {3.6,   4.1},
                {3.9,   4.25},
                {4.2,   4.86},
                {4.5,   3.66},
                {4.8,   4.93},
                {5.1,   5.1},
                {5.4,   5.8900000000000006},
                {5.7,   5.59},
                {6, 5.27},
                {6.3,   6.95},
                {6.6,   6.2399999999999993},
                {6.9,   7.3800000000000008},
                {7.2,   7.24},
                {7.5,   7.83},
                {7.8,   8.2},
                {8.1,   7.46},
                {8.4,   9.370000000000001},
                {8.7,   8.0499999999999989},
                {9, 8.72},
                {9.3,   9.71},
                {9.6,   9.7199999999999989},
                {9.9,   9.18},
                {10.2,  10.04},
                {10.5,  9.81},
                {10.8,  10.82},
                {11.1,  10.69},
                {11.4,  10.93},
                {11.7,  12.11},
                {12,    11.43},
                {12.3,  11.510000000000002},
                {12.6,  12.56},
                {12.9,  13.55},
                {13.2,  12.219999999999999},
                {13.5,  14.33},
                {13.8,  13.530000000000001},
                {14.1,  14.92},
                {14.4,  14.030000000000001},
                {14.7,  14.799999999999999},
                {15,    14.69},
                {15.3,  14.42},
                {15.6,  15.54},
                {1, 13.9},
                {2, 19.6},
                {3, 14.1},
                {4, 11.8},
                {5, 0.1},
                {6, 12.8},
                {7, 9.3},
                {8, 2.6},
                {9, 10.9},
                {10,    4.7},
                {11,    9.4},
                {12,    2.4},
                {13,    1.3},
                {14,    9.1},
                {2, 10},
                {3, 9},
                {15,    2},
                {15.5,  1.2}
            };
            double[][] jaggedRansacIn = ransacIn.ToJagged();

//FOREST

            // prog.initTree(jaggedInputs, outputs);
            //prog.testTree(jaggedInputs, outputs);

//RANSAC

            //prog.ransac(jaggedRansacIn);

            //posit setup
            accord.PositTest pTest = new accord.PositTest();

            //model points for sample (cube)
            Vector3[] model = new Vector3[4]
            {
            new Vector3 (28,28,-28),
            new Vector3(-28,28,-28),
            new Vector3(28,-28,-28),
            new Vector3  (28,28,28),
            };

            //focal length of camera (find a way to estimate this)
            float fl = 640;
            
//POSIT

            //pTest.runPosit(model, fl);
//ICP
            accord.icp icpImp = new accord.icp();
            icpImp.runICP();
        }
    }
}
