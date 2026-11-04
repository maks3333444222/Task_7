using System;
using System.Globalization;
using System.Text;

namespace Practice07
{
    // Работа №7, вариант 3. Всё состояние передаётся параметрами: полей нет.
    public class Program
    {
        public static void Main()
        {
            ConfigureConsole();
            RunInspectionDemo();
            RunDispensingDemo();
            RunInfectionDemo();
            RunClimateDemo();
            RunCanopyDemo();
        }

        private static void ConfigureConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            Console.WriteLine("ПРАКТИЧЕСКАЯ №7 • ВАРИАНТ 3 • АГРО-ДРОН");
        }

        // bool — выполнен ли осмотр, ref — изменяемые ресурсы, out — результаты.
        public static bool ProcessPlantInspection(ref int batteryCharge,
            ref double tankNutrientMl, int leafMaturityIndex,
            out bool needsSpraying, out string warningCode)
        {
            needsSpraying = false;
            warningCode = "INVALID_INPUT";
            if (batteryCharge < 0 || batteryCharge > 100 ||
                !double.IsFinite(tankNutrientMl) || tankNutrientMl < 0 ||
                leafMaturityIndex < 0 || leafMaturityIndex > 100)
                return false;

            needsSpraying = leafMaturityIndex < 40;
            if (batteryCharge < 2)
            {
                warningCode = "LOW_BATTERY";
                return false;
            }
            if (needsSpraying && tankNutrientMl < 20)
            {
                warningCode = "EMPTY_TANK";
                return false;
            }

            // После проверок обе операции гарантированно выполнимы.
            if (needsSpraying) DispenseSolution(ref tankNutrientMl, 20.0);
            batteryCharge -= 2;
            warningCode = needsSpraying ? "LOW_MATURITY" : "OK";
            return true;
        }

        // Перегрузка 1: известен конкретный объём.
        public static bool DispenseSolution(ref double tankMl, double doseMl)
        {
            if (!double.IsFinite(tankMl) || !double.IsFinite(doseMl) ||
                tankMl < 0 || doseMl < 0 || doseMl > tankMl)
                return false;
            tankMl -= doseMl;
            return true;
        }

        // Перегрузка 2: объём зависит от площади и уровня заражения 0..10.
        public static bool DispenseSolution(ref double tankMl, double areaSqM,
            int pestLevel, out double finalConcentration)
        {
            finalConcentration = 0;
            if (!double.IsFinite(areaSqM) || areaSqM <= 0 ||
                pestLevel < 0 || pestLevel > 10)
                return false;
            double doseMl = areaSqM * 25 * (1 + pestLevel / 10.0);
            if (!DispenseSolution(ref tankMl, doseMl)) return false;
            finalConcentration = 0.5 + pestLevel * 0.1; // проценты
            return true;
        }

        public static int PredictInfectionSpread(int initialPlants,
            double humidity, int hoursRemaining)
        {
            if (initialPlants < 0 || !double.IsFinite(humidity) ||
                humidity < 0 || humidity > 100 ||
                hoursRemaining < 0 || hoursRemaining > 24)
                throw new ArgumentOutOfRangeException(nameof(initialPlants),
                    "Нужны растения >= 0, влажность 0..100 и часы 0..24.");

            // Базовый случай завершает рекурсивный спуск.
            if (hoursRemaining == 0 || initialPlants == 0) return initialPlants;
            double next = Math.Ceiling(initialPlants * (1 + humidity / 1000.0));
            if (next > int.MaxValue)
                throw new OverflowException("Число растений вышло за диапазон int.");
            return PredictInfectionSpread((int)next, humidity, hoursRemaining - 1);
        }

        // Запрошенная в варианте форма вызова с params.
        public static double CalculateZoneQuality(int optimalTemp, params int[] zoneTemps)
        {
            if (!TryCalculateZoneQuality(optimalTemp, zoneTemps, out double deviation))
                throw new ArgumentException("Нужен хотя бы один замер температуры.", nameof(zoneTemps));
            return deviation;
        }

        // Для анализа также предусмотрен контракт bool + out из общих требований.
        public static bool TryCalculateZoneQuality(int optimalTemp, int[]? zoneTemps,
            out double deviation)
        {
            deviation = 0;
            if (zoneTemps == null || zoneTemps.Length == 0) return false;
            double sumSquares = 0;
            foreach (int temp in zoneTemps)
            {
                double difference = (double)temp - optimalTemp;
                sumSquares += difference * difference;
            }
            deviation = Math.Sqrt(sumSquares / zoneTemps.Length);
            return true;
        }

        public static double[,] CreateCanopyMatrix(int rows, int columns, int seed)
        {
            if (rows <= 0 || columns <= 0)
                throw new ArgumentOutOfRangeException(nameof(rows));
            Random random = new Random(seed);
            double[,] matrix = new double[rows, columns];
            for (int row = 0; row < matrix.GetLength(0); row++)
                for (int column = 0; column < matrix.GetLength(1); column++)
                    matrix[row, column] = Math.Round(random.NextDouble(), 2);
            return matrix;
        }

        public static bool AnalyzeCanopyMatrix(ref double[,] ndviMatrix,
            out int healthiestRow, out int healthiestColumn, out double maxIndex)
        {
            healthiestRow = -1;
            healthiestColumn = -1;
            maxIndex = 0;
            if (ndviMatrix == null || ndviMatrix.Length == 0) return false;

            // Сначала проверяем ВСЮ матрицу: ошибка не оставит её наполовину изменённой.
            for (int row = 0; row < ndviMatrix.GetLength(0); row++)
                for (int column = 0; column < ndviMatrix.GetLength(1); column++)
                {
                    double value = ndviMatrix[row, column];
                    if (!double.IsFinite(value) || value < -1 || value > 1) return false;
                }

            for (int row = 0; row < ndviMatrix.GetLength(0); row++)
                for (int column = 0; column < ndviMatrix.GetLength(1); column++)
                {
                    if (ndviMatrix[row, column] < 0.3)
                        ndviMatrix[row, column] = 0; // Изменение исходной матрицы.
                    else if (healthiestRow == -1 || ndviMatrix[row, column] > maxIndex)
                    {
                        maxIndex = ndviMatrix[row, column];
                        healthiestRow = row;
                        healthiestColumn = column;
                    }
                }

            PrintCanopyMatrix(ndviMatrix);
            return healthiestRow != -1;
        }

        public static void PrintCanopyMatrix(double[,] matrix)
        {
            ConsoleColor previous = Console.ForegroundColor;
            for (int row = 0; row < matrix.GetLength(0); row++)
            {
                for (int column = 0; column < matrix.GetLength(1); column++)
                {
                    double value = matrix[row, column];
                    if (!Console.IsOutputRedirected)
                        Console.ForegroundColor = value == 0 ? ConsoleColor.Red :
                            value >= 0.6 ? ConsoleColor.Green : ConsoleColor.Yellow;
                    Console.Write($"{value,7:F2}");
                }
                Console.WriteLine();
            }
            if (!Console.IsOutputRedirected) Console.ForegroundColor = previous;
        }

        private static void RunInspectionDemo()
        {
            Console.WriteLine("\n1. Осмотр и расход ресурсов");
            int battery = 80;
            double tank = 300;
            foreach (int maturity in new int[] { 25, 70, 35 })
            {
                bool ok = ProcessPlantInspection(ref battery, ref tank, maturity,
                    out bool spray, out string warning);
                Console.WriteLine($"Зрелость {maturity}: успех={ok}, опрыскивание={spray}, " +
                    $"код={warning}, заряд={battery}%, раствор={tank:F1} мл");
            }
        }

        private static void RunDispensingDemo()
        {
            Console.WriteLine("\n2. Две перегрузки дозирования");
            double tank = 200;
            bool first = DispenseSolution(ref tank, 15);
            bool second = DispenseSolution(ref tank, 2, 4, out double concentration);
            Console.WriteLine($"Точечная доза: {first}; обработка площади: {second}; " +
                $"остаток={tank:F1} мл, концентрация={concentration:F1}%");
        }

        private static void RunInfectionDemo()
        {
            Console.WriteLine("\n3. Рекурсивный прогноз");
            Console.WriteLine($"Из 10 очагов при влажности 80% через 4 ч: " +
                $"{PredictInfectionSpread(10, 80, 4)} растений.");
        }

        private static void RunClimateDemo()
        {
            Console.WriteLine("\n4. Анализ температур через params");
            Console.WriteLine($"Отклонение от 24 °C: {CalculateZoneQuality(24, 22, 24, 26):F2} °C");
        }

        private static void RunCanopyDemo()
        {
            Console.WriteLine("\n5. Матрица NDVI после обработки (индексы с нуля)");
            double[,] canopy = CreateCanopyMatrix(3, 4, 42);
            bool ok = AnalyzeCanopyMatrix(ref canopy, out int row, out int column, out double max);
            Console.WriteLine(ok ? $"Самая здоровая ячейка: [{row}, {column}], NDVI={max:F2}" :
                "Здоровые ячейки не найдены либо входные данные некорректны.");
        }
    }
}
