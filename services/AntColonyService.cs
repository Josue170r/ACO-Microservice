using ACO_Microservice.Models.Requests;
using ACO_Microservice.Models.Responses;

namespace ACO_Microservice.Services
{
    public class AntColonyService : IAntColonyService
    {
        // ACO Parameters (from Python code)
        private const int NUM_ANTS = 20;
        private const int NUM_ITERATIONS = 50;
        private const double ALPHA = 1.0; // Pheromone importance
        private const double BETA = 2.0;  // Heuristic importance
        private const double RHO = 0.5;   // Evaporation rate
        private const double Q = 100;     // Pheromone deposit factor
        private const double INITIAL_PHEROMONE = 1.0;

        // Time constraints
        private const int START_HOUR = 9;
        private const int END_HOUR = 21;
        private const int LUNCH_START = 12;
        private const int LUNCH_END = 14;

        // Duration by place type (hours)
        private readonly Dictionary<string, double> DurationByType = new()
        {
            ["museum"] = 2.5,
            ["art_gallery"] = 2.5,
            ["zoo"] = 3.0,
            ["aquarium"] = 2.0,
            ["amusement_park"] = 4.0,
            ["restaurant"] = 1.5,
            ["cafe"] = 1.0,
            ["bar"] = 1.5,
            ["park"] = 1.5,
            ["beach"] = 2.0,
            ["shopping_mall"] = 2.0,
            ["spa"] = 2.0,
            ["tourist_attraction"] = 2.0
        };

        private double[,] pheromones = default!;
        private double[,] distances = default!;
        private Random random = new Random();

        public SaveItineraryRequest OptimizeItinerary(
            PlaceData hotel,
            List<PlaceData> places,
            DateTime startDate,
            DateTime endDate,
            string tripTitle,
            bool isCertificatedHotel)
        {
            var totalDays = (endDate - startDate).Days + 1;
            var allPlaces = new List<PlaceData> { hotel };
            allPlaces.AddRange(places);

            // Initialize matrices
            InitializeMatrices(allPlaces);

            // Run ACO algorithm
            var bestSolution = RunACO(allPlaces, totalDays);

            // Convert solution to itinerary request
            return ConvertToItineraryRequest(bestSolution, allPlaces, startDate, totalDays, tripTitle, hotel.Id, isCertificatedHotel);
        }

        private void InitializeMatrices(List<PlaceData> places)
        {
            int n = places.Count;
            pheromones = new double[n, n];
            distances = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i != j)
                    {
                        distances[i, j] = CalculateDistance(places[i], places[j]);
                        pheromones[i, j] = INITIAL_PHEROMONE;
                    }
                }
            }
        }

        private double CalculateDistance(PlaceData place1, PlaceData place2)
        {
            // Haversine formula
            const double R = 6371; // Earth radius in km
            double lat1 = place1.Lat * Math.PI / 180;
            double lat2 = place2.Lat * Math.PI / 180;
            double deltaLat = (place2.Lat - place1.Lat) * Math.PI / 180;
            double deltaLng = (place2.Lng - place1.Lng) * Math.PI / 180;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                      Math.Cos(lat1) * Math.Cos(lat2) *
                      Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private List<List<int>> RunACO(List<PlaceData> places, int totalDays)
        {
            var bestSolution = new List<List<int>>();
            double bestFitness = double.MinValue;

            for (int iteration = 0; iteration < NUM_ITERATIONS; iteration++)
            {
                var solutions = new List<(List<List<int>> solution, double fitness)>();

                // Each ant builds a solution
                for (int ant = 0; ant < NUM_ANTS; ant++)
                {
                    var solution = BuildSolution(places, totalDays);
                    var fitness = EvaluateSolution(solution, places);
                    solutions.Add((solution, fitness));

                    if (fitness > bestFitness)
                    {
                        bestFitness = fitness;
                        bestSolution = solution;
                    }
                }

                // Update pheromones
                UpdatePheromones(solutions, places);
            }

            return bestSolution;
        }

        private List<List<int>> BuildSolution(List<PlaceData> places, int totalDays)
        {
            var solution = new List<List<int>>();
            var visited = new HashSet<int> { 0 };
            var placesPerDay = Math.Min(4, Math.Max(2, places.Count / totalDays));

            for (int day = 0; day < totalDays; day++)
            {
                var dayPlan = new List<int>();
                int currentPlace = 0;
                double currentTime = START_HOUR;

                while (currentTime < END_HOUR - 2 && dayPlan.Count < placesPerDay)
                {
                    int nextPlace = SelectNextPlace(currentPlace, visited, places);
                    if (nextPlace == -1) break;

                    if (visited.Contains(nextPlace))
                    {
                        continue;
                    }

                    dayPlan.Add(nextPlace);
                    visited.Add(nextPlace);

                    var placeType = places[nextPlace].PlaceTypes.FirstOrDefault() ?? "tourist_attraction";
                    currentTime += DurationByType.GetValueOrDefault(placeType, 2.0);
                    currentTime += 0.5;

                    if (currentTime >= LUNCH_START && currentTime < LUNCH_END)
                    {
                        currentTime = LUNCH_END + 0.5;
                    }

                    currentPlace = nextPlace;
                }

                if (dayPlan.Any())
                {
                    solution.Add(dayPlan);
                }
            }
            return solution;
        }

        private int SelectNextPlace(int currentPlace, HashSet<int> visited, List<PlaceData> places)
        {
            var probabilities = new List<(int index, double probability)>();
            double totalProbability = 0;

            for (int i = 1; i < places.Count; i++) // Skip hotel at index 0
            {
                if (!visited.Contains(i))
                {
                    double pheromone = Math.Pow(pheromones[currentPlace, i], ALPHA);
                    double heuristic = Math.Pow(1.0 / distances[currentPlace, i], BETA);

                    // Include sustainability as part of heuristic
                    heuristic *= (1 + places[i].SustainabilityIndex);

                    // Include rating as part of heuristic
                    heuristic *= (1 + places[i].Rating / 5.0);

                    double probability = pheromone * heuristic;
                    probabilities.Add((i, probability));
                    totalProbability += probability;
                }
            }

            if (probabilities.Count == 0) return -1;

            // Roulette wheel selection
            double randomValue = random.NextDouble() * totalProbability;
            double cumulative = 0;

            foreach (var (index, probability) in probabilities)
            {
                cumulative += probability;
                if (randomValue <= cumulative)
                {
                    return index;
                }
            }

            return probabilities.Last().index;
        }

        private double EvaluateSolution(List<List<int>> solution, List<PlaceData> places)
        {
            double fitness = 0;

            foreach (var day in solution)
            {
                // Evaluate distance
                double dayDistance = 0;
                int prevPlace = 0; // Hotel

                foreach (int placeIndex in day)
                {
                    dayDistance += distances[prevPlace, placeIndex];
                    prevPlace = placeIndex;
                }
                dayDistance += distances[prevPlace, 0]; // Return to hotel

                // Penalize long distances
                fitness -= dayDistance * 0.1;

                // Reward sustainability
                foreach (int placeIndex in day)
                {
                    fitness += places[placeIndex].SustainabilityIndex * 10;
                    fitness += places[placeIndex].Rating;
                }

                // Reward balanced days
                fitness += day.Count * 2;
            }

            return fitness;
        }

        private void UpdatePheromones(List<(List<List<int>> solution, double fitness)> solutions, List<PlaceData> places)
        {
            // Evaporation
            for (int i = 0; i < places.Count; i++)
            {
                for (int j = 0; j < places.Count; j++)
                {
                    pheromones[i, j] *= (1 - RHO);
                }
            }

            // Deposit pheromones
            foreach (var (solution, fitness) in solutions)
            {
                double deposit = Q * fitness;

                foreach (var day in solution)
                {
                    int prevPlace = 0;
                    foreach (int placeIndex in day)
                    {
                        pheromones[prevPlace, placeIndex] += deposit;
                        prevPlace = placeIndex;
                    }
                    if (day.Any())
                    {
                        pheromones[prevPlace, 0] += deposit;
                    }
                }
            }
        }

        private SaveItineraryRequest ConvertToItineraryRequest(
            List<List<int>> solution,
            List<PlaceData> places,
            DateTime startDate,
            int totalDays,
            string tripTitle,
            long hotelPlaceId,
            bool isCertificatedHotel)
        {
            var request = new SaveItineraryRequest
            {
                TripTitle = tripTitle,
                HotelPlaceId = hotelPlaceId,
                IsCertificatedHotel = isCertificatedHotel,
                ItineraryDays = new List<ItineraryDayDto>()
            };

            for (int day = 0; day < totalDays && day < solution.Count; day++)
            {
                var dayDto = new ItineraryDayDto
                {
                    ItineraryDate = startDate.AddDays(day).ToString("yyyy-MM-dd"),
                    Places = new List<PlaceVisitDto>()
                };

                var currentTime = new TimeSpan(START_HOUR, 0, 0);
                int visitOrder = 1;

                foreach (int placeIndex in solution[day])
                {
                    var place = places[placeIndex];
                    var placeType = place.PlaceTypes.FirstOrDefault() ?? "tourist_attraction";
                    var duration = TimeSpan.FromHours(DurationByType.GetValueOrDefault(placeType, 2.0));

                    var arrivalTime = currentTime;
                    var leavingTime = currentTime.Add(duration);

                    // Handle lunch break
                    if (arrivalTime.Hours >= LUNCH_START && arrivalTime.Hours < LUNCH_END)
                    {
                        arrivalTime = new TimeSpan(LUNCH_END, 30, 0);
                        leavingTime = arrivalTime.Add(duration);
                    }

                    dayDto.Places.Add(new PlaceVisitDto
                    {
                        PlaceId = place.Id,
                        PostalCode = place.PostalCode ?? "00000",
                        VisitOrder = visitOrder++,
                        ArrivalTime = $"{arrivalTime.Hours:D2}:{arrivalTime.Minutes:D2}",
                        LeavingTime = $"{leavingTime.Hours:D2}:{leavingTime.Minutes:D2}"
                    });

                    currentTime = leavingTime.Add(TimeSpan.FromMinutes(30));
                }

                request.ItineraryDays.Add(dayDto);
            }

            return request;
        }
    }
}