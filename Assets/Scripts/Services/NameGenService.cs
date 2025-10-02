using System;
using System.Collections.Generic;
using UnityEngine;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Services
{
    public class NameGenService : IDisposable
    {
        #region Constants

        private const string CLASS_NAME = nameof(NameGenService);

        #endregion // Constants

        #region Singleton

        /// <summary>
        /// Singleton instance of the NameGenService service.
        /// </summary>
        private static NameGenService instance;
        public static NameGenService Instance
        {
            get
            {
                instance ??= new NameGenService();
                return instance;
            }
        }

        #endregion // Singleton

        #region Properties

        /// <summary>
        /// Indicates if the service is properly initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Indicates if the service has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        public List<string> ArabicLastNames => arabicLastNames;
        #endregion

        #region Private Fields
        private System.Random random;
        private bool enableDebugLogging = false;

        // Russian male first names
        private readonly List<string> russianMaleFirstNames = new()
        {
            "Aleksandr", "Aleksei", "Anatoli", "Andrei", "Anton",
            "Boris", "Dmitri", "Fyodor", "Gennadi", "Georgi",
            "Igor", "Ivan", "Konstantin", "Leonid", "Maxim",
            "Mikhail", "Nikolai", "Oleg", "Pavel", "Pyotr",
            "Roman", "Sergei", "Stanislav", "Valeri", "Vasili",
            "Viktor", "Vladimir", "Vladislav", "Yegor", "Yuri"
        };

        // Russian last names
        private readonly List<string> russianLastNames = new()
        {
            "Ivanov", "Smirnov", "Kuznetsov", "Popov", "Vasiliev",
            "Petrov", "Sokolov", "Mikhailov", "Novikov", "Fedorov",
            "Morozov", "Volkov", "Alekseev", "Lebedev", "Semenov",
            "Egorov", "Pavlov", "Kozlov", "Stepanov", "Nikolaev",
            "Orlov", "Andreev", "Makarov", "Nikitin", "Zakharov",
            "Zaytsev", "Solovyov", "Borisov", "Yakovlev", "Grigoriev",
            "Romanov", "Vorobiev", "Sidorov", "Kuzmin", "Fomin",
            "Baranov", "Filippov", "Davidov", "Belyaev", "Tarasov",
            "Golubev", "Bogdanov", "Medvedev", "Belov", "Antonov",
            "Gusev", "Rodionov", "Komarov", "Polyakov", "Konovalov"
        };

        // US male first names
        private readonly List<string> usMaleFirstNames = new()
        {
            "James", "John", "Robert", "Michael", "William",
            "David", "Richard", "Joseph", "Thomas", "Charles",
            "Christopher", "Daniel", "Matthew", "Anthony", "Mark",
            "Donald", "Steven", "Paul", "Andrew", "Joshua",
            "Kenneth", "Kevin", "Brian", "George", "Timothy",
            "Ronald", "Edward", "Jason", "Jeffrey", "Ryan"
        };

        // US last names
        private readonly List<string> usLastNames = new()
        {
            "Smith", "Johnson", "Williams", "Jones", "Brown",
            "Davis", "Miller", "Wilson", "Moore", "Taylor",
            "Anderson", "Thomas", "Jackson", "White", "Harris",
            "Martin", "Thompson", "Garcia", "Martinez", "Robinson",
            "Clark", "Rodriguez", "Lewis", "Lee", "Walker",
            "Hall", "Allen", "Young", "Hernandez", "King",
            "Wright", "Lopez", "Hill", "Scott", "Green",
            "Adams", "Baker", "Gonzalez", "Nelson", "Carter",
            "Mitchell", "Perez", "Roberts", "Turner", "Phillips",
            "Campbell", "Parker", "Evans", "Edwards", "Collins"
        };

        // UK male first names
        private readonly List<string> ukMaleFirstNames = new()
        {
            "Oliver", "George", "Harry", "Jack", "Charlie",
            "Leo", "Jacob", "Freddie", "Alfie", "Oscar",
            "Arthur", "Henry", "William", "Thomas", "James",
            "Theo", "Noah", "Edward", "Ethan", "Lucas",
            "Alexander", "Benjamin", "Mason", "Harrison", "Logan",
            "Daniel", "Isaac", "Joseph", "Samuel", "Sebastian"
        };

        // UK last names
        private readonly List<string> ukLastNames = new()
        {
            "Smith", "Jones", "Williams", "Taylor", "Brown",
            "Davies", "Evans", "Wilson", "Thomas", "Roberts",
            "Johnson", "Lewis", "Walker", "Robinson", "Wood",
            "Thompson", "White", "Watson", "Jackson", "Wright",
            "Green", "Harris", "Cooper", "King", "Lee",
            "Martin", "Clarke", "James", "Morgan", "Hughes",
            "Edwards", "Hill", "Moore", "Clark", "Harrison",
            "Scott", "Young", "Morris", "Hall", "Ward",
            "Turner", "Carter", "Phillips", "Mitchell", "Patel",
            "Adams", "Campbell", "Anderson", "Allen", "Cook"
        };

        // German male first names
        private readonly List<string> germanMaleFirstNames = new()
        {
            "Alexander", "Maximilian", "Paul", "Leon", "Felix",
            "Lukas", "Elias", "Jonas", "David", "Julian",
            "Moritz", "Philipp", "Tim", "Niklas", "Jakob",
            "Fabian", "Jan", "Ben", "Jonathan", "Simon",
            "Florian", "Luca", "Finn", "Daniel", "Sebastian",
            "Marcel", "Christian", "Thomas", "Andreas", "Stefan"
        };

        // German last names
        private readonly List<string> germanLastNames = new()
        {
            "Müller", "Schmidt", "Schneider", "Fischer", "Weber",
            "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann",
            "Schäfer", "Koch", "Bauer", "Richter", "Klein",
            "Wolf", "Schröder", "Neumann", "Schwarz", "Zimmermann",
            "Braun", "Krüger", "Hofmann", "Hartmann", "Lange",
            "Schmitt", "Werner", "Schmitz", "Krause", "Meier",
            "Lehmann", "Schmid", "Schulze", "Maier", "Köhler",
            "Herrmann", "König", "Walter", "Mayer", "Huber",
            "Kaiser", "Fuchs", "Peters", "Lang", "Scholz",
            "Möller", "Weiß", "Jung", "Hahn", "Schubert"
        };

        // French male first names
        private readonly List<string> frenchMaleFirstNames = new()
        {
            "Jean", "Pierre", "Michel", "André", "Philippe",
            "Louis", "Nicolas", "François", "Henri", "Bernard",
            "Jacques", "Paul", "Marcel", "Robert", "Claude",
            "Daniel", "Christian", "Thomas", "Joseph", "Alain",
            "Antoine", "Maurice", "Christophe", "Vincent", "Guillaume",
            "Alexandre", "Julien", "Sébastien", "Patrick", "David"
        };

        // French last names
        private readonly List<string> frenchLastNames = new()
        {
            "Martin", "Bernard", "Thomas", "Petit", "Robert",
            "Richard", "Durand", "Dubois", "Moreau", "Laurent",
            "Simon", "Michel", "Lefebvre", "Leroy", "Roux",
            "David", "Bertrand", "Morel", "Fournier", "Girard",
            "Bonnet", "Dupont", "Lambert", "Fontaine", "Rousseau",
            "Vincent", "Muller", "Lefevre", "Faure", "Andre",
            "Mercier", "Blanc", "Guerin", "Boyer", "Garnier",
            "Chevalier", "Francois", "Legrand", "Gauthier", "Garcia",
            "Perrin", "Robin", "Clement", "Morin", "Nicolas",
            "Henry", "Roussel", "Mathieu", "Gautier", "Masson"
        };

        // Arabic male first names
        private readonly List<string> arabicMaleFirstNames = new()
        {
            "Mohammed", "Ahmed", "Ali", "Omar", "Abdullah",
            "Yusuf", "Ibrahim", "Hassan", "Husain", "Khalid",
            "Mahmoud", "Mustafa", "Abdul", "Samir", "Said",
            "Tariq", "Waleed", "Bilal", "Faisal", "Majid",
            "Adil", "Karim", "Jamal", "Hamza", "Malik",
            "Nasser", "Rashid", "Salim", "Ziad", "Fahad"
        };

        // Arabic last names
        private readonly List<string> arabicLastNames = new()
        {
            "Al-Saud", "Al-Ghamdi", "Al-Qahtan", "Al-Shammari", "Al-Otaibi",
            "Al-Qahtani", "Al-Dossari", "Al-Mutairi", "Al-Harbi", "Al-Subaie",
            "Al-Zahrani", "Al-Anazi", "Al-Shehri", "Al-Maliki", "Al-Juhani",
            "Al-Faraj", "Al-Yami", "Al-Hamad", "Al-Salem", "Al-Hajri",
            "Al-Balawi", "Al-Saleh", "Al-Rashidi", "Al-Ahmed", "Al-Asiri",
            "Al-Taweel", "Al-Qurashi", "Al-Amri", "Al-Dawsari", "Al-Khalidi",
            "Al-Saeed", "Al-Asmari", "Al-Sharif", "Al-Yahya", "Al-Harithi",
            "Al-Jaber", "Al-Mahdi", "Al-Najjar", "Al-Omari", "Al-Tamimi",
            "Al-Ruwaili", "Al-Harthy", "Al-Zaidi", "Al-Harthi", "Al-Dosari",
            "Al-Hassan", "Al-Bishi", "Al-Qurayshi", "Al-Farsi", "Al-Ammar"
        };

        #endregion // Private Fields

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the NameGenService service.
        /// </summary>
        private NameGenService()
        {
            try
            {
                Initialize();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        #endregion // Constructor

        #region Initialization

        /// <summary>
        /// Initializes the name generator service.
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Initialize random number generator
                random = new System.Random();

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Service initialized successfully");
                }

                IsInitialized = true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Initialize", e);
                IsInitialized = false;
                throw;
            }
        }

        #endregion // Initialization

        #region Public Methods

        /// <summary>
        /// Generates a random male name based on the given nationality.
        /// </summary>
        /// <param name="nationality">The nationality to generate a name for</param>
        /// <returns>A full name string (first and last name)</returns>
        public string GenerateMaleName(Nationality nationality)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"{CLASS_NAME} is not initialized.");
            }

            if (IsDisposed)
            {
                throw new ObjectDisposedException(CLASS_NAME);
            }

            try
            {
                string firstName = GenerateMaleFirstName(nationality);
                string lastName = GenerateLastName(nationality);
                return $"{firstName} {lastName}";
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GenerateMaleName", e);
                throw;
            }
        }

        /// <summary>
        /// Generates a random male first name based on the given nationality.
        /// </summary>
        /// <param name="nationality">The nationality to generate a name for</param>
        /// <returns>A first name string</returns>
        public string GenerateMaleFirstName(Nationality nationality)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"{CLASS_NAME} is not initialized.");
            }

            if (IsDisposed)
            {
                throw new ObjectDisposedException(CLASS_NAME);
            }

            try
            {
                List<string> firstNames = GetMaleFirstNameList(nationality);
                return firstNames[random.Next(firstNames.Count)];
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GenerateMaleFirstName", e);
                throw;
            }
        }

        /// <summary>
        /// Generates a random last name based on the given nationality.
        /// </summary>
        /// <param name="nationality">The nationality to generate a name for</param>
        /// <returns>A last name string</returns>
        public string GenerateLastName(Nationality nationality)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"{CLASS_NAME} is not initialized.");
            }

            if (IsDisposed)
            {
                throw new ObjectDisposedException(CLASS_NAME);
            }

            try
            {
                List<string> lastNames = GetLastNameList(nationality);
                return lastNames[random.Next(lastNames.Count)];
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GenerateLastName", e);
                throw;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Gets the appropriate list of male first names for the given nationality.
        /// </summary>
        /// <param name="nationality">The nationality to get names for</param>
        /// <returns>A list of first names appropriate for the nationality</returns>
        private List<string> GetMaleFirstNameList(Nationality nationality)
        {
            return nationality switch
            {
                Nationality.USSR => russianMaleFirstNames,
                Nationality.USA => usMaleFirstNames,
                Nationality.UK => ukMaleFirstNames,
                Nationality.FRG => germanMaleFirstNames,
                Nationality.FRA => frenchMaleFirstNames,
                Nationality.IR or Nationality.IQ or Nationality.SAUD => arabicMaleFirstNames,
                Nationality.MJ => russianMaleFirstNames,// Default to Russian for "MJ" (assuming this is mostly Russian)
                _ => russianMaleFirstNames,// Default to Russian names
            };
        }

        /// <summary>
        /// Gets the appropriate list of last names for the given nationality.
        /// </summary>
        /// <param name="nationality">The nationality to get names for</param>
        /// <returns>A list of last names appropriate for the nationality</returns>
        private List<string> GetLastNameList(Nationality nationality)
        {
            return nationality switch
            {
                Nationality.USSR => russianLastNames,
                Nationality.USA => usLastNames,
                Nationality.UK => ukLastNames,
                Nationality.FRG => germanLastNames,
                Nationality.FRA => frenchLastNames,
                Nationality.IR or Nationality.IQ or Nationality.SAUD => ArabicLastNames,
                Nationality.MJ => russianLastNames,// Default to Russian for "MJ" (assuming this is mostly Russian)
                _ => russianLastNames,// Default to Russian names
            };
        }

        #endregion // Private Methods

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by the name generator service.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the name generator service and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources here
                    if (enableDebugLogging)
                    {
                        Debug.Log($"{CLASS_NAME}: Service disposed.");
                    }
                }

                IsDisposed = true;

                // Clear singleton reference if this is the main instance
                if (instance == this)
                {
                    instance = null;
                }
            }
        }

        /// <summary>
        /// Finalizer for the name generator service.
        /// </summary>
        ~NameGenService()
        {
            Dispose(false);
        }

        #endregion // IDisposable Implementation
    }
}