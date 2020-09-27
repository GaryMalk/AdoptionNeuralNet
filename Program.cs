using SharpLearning.Metrics.Regression;
using SharpLearning.Neural;
using SharpLearning.Neural.Activations;
using SharpLearning.Neural.Layers;
using SharpLearning.Neural.Learners;
using SharpLearning.Neural.Loss;
using System;
using System.Configuration;
using System.IO;

namespace AdoptionNeuralNet
{
    class Program
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["AdoptionStatistics"].ConnectionString;
        private static string outputDirectory = ConfigurationManager.AppSettings["outputModels"];

        static void Main(string[] args)
        {
            string[] races = { "Black",  "Hispanic", "NativeAmerican", "White" };
            foreach (string race in races)
            {
                var dataTable = Utilities.GetDataTable(
                    $@"SELECT [StateId]
                          ,[Year]
                          ,[Subsidy]
                          ,[Married]
                          ,[AverageAge]
                          ,[Male]
                          ,[{race}]
                          ,[NonRelative]
                          ,[AverageMonths]
                    FROM[AdoptionStatistics].[dbo].[ObservationsByStateYear]", connectionString);

                // do one hot encoding before we split the table up into training and test
                // otherwise we will have a problem if the StateId column has different number of distict values,
                // which it actually does for 2016: data for Puerto Rico 2016 is not available
                dataTable = Utilities.OneHotEncoder(dataTable, new string[] { "StateId" });

                // get training data, all years except most recent, 2016
                var trainingRows = dataTable.Select("[Year] <> 2016");
                // get most recent year, 2016, for evaluating model
                var testRows = dataTable.Select("[Year] = 2016");

                (var observationsTraining, var targetsTraining) = Utilities.ObservationsFromDataRows(trainingRows, "AverageMonths");
                (var observationsTest, var targetsTest) = Utilities.ObservationsFromDataRows(testRows, "AverageMonths");

                // create neural net
                var adoptStatistics = new NeuralNet();
                adoptStatistics.Add(new InputLayer(inputUnits: dataTable.Columns.Count - 1));
                adoptStatistics.Add(new DenseLayer(1200, Activation.Relu));
                adoptStatistics.Add(new DenseLayer(1200, Activation.Relu));
                adoptStatistics.Add(new DenseLayer(1200, Activation.Relu));
                adoptStatistics.Add(new DenseLayer(1200, Activation.Relu));
                adoptStatistics.Add(new SquaredErrorRegressionLayer());

                // create learner, set iterations
                var learner = new RegressionNeuralNetLearner(adoptStatistics, iterations: 1000, loss: new SquareLoss());
                var model = learner.Learn(observationsTraining, targetsTraining);

                DirectoryInfo directory = new DirectoryInfo(outputDirectory);
                if (!directory.Exists)
                {
                    directory.Create();
                }

                string modelName = $"adoptModel_{race}_{DateTime.Now:yy.MM.dd.HH.mm.ss}";
                model.Save(() => new StreamWriter(Path.Combine(directory.FullName, $"{modelName}.xml")));
                var metric = new MeanSquaredErrorRegressionMetric();

                var predictionsTraining = model.Predict(observationsTraining);
                var errorTraining = metric.Error(targetsTraining, predictionsTraining);
                Console.WriteLine($"Training Error for {modelName}: {errorTraining}");

                var predictionsTest = model.Predict(observationsTest);
                var errorTest = metric.Error(targetsTest, predictionsTest);
                Console.WriteLine($"Evaluation Error for {modelName}: {errorTest}");
            }
        }
    }
}
