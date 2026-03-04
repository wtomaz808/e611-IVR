using IVR.Core.Models;

namespace IVR.DataSeeder;

public static class AniAliSeeder
{
    /// <summary>
    /// Generates comprehensive test ANI (Automatic Number Identification) records
    /// </summary>
    public static List<AniRecord> GenerateTestAniRecords()
    {
        return new List<AniRecord>
        {
            // === RESIDENTIAL CALLERS ===
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15551234567",
                CallerName = "John Smith",
                CallerType = CallerType.Residential,
                Language = "en-US",
                Priority = 0,
                IsVip = false,
                IsBlocked = false
            },
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15552345678",
                CallerName = "Maria Garcia",
                CallerType = CallerType.Residential,
                Language = "es-US",
                Priority = 0,
                IsVip = false,
                IsBlocked = false
            },
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15553456789",
                CallerName = "Chen Wei",
                AccountNumber = "RES-98765",
                CallerType = CallerType.Residential,
                Language = "en-US",
                Priority = 0,
                IsVip = false,
                IsBlocked = false
            },

            // === BUSINESS CALLERS ===
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15559876543",
                CallerName = "City Hall - Mayor's Office",
                AccountNumber = "BUS-54321",
                CallerType = CallerType.Business,
                Language = "en-US",
                Priority = 5,
                IsVip = true,
                IsBlocked = false,
                Metadata = new Dictionary<string, string>
                {
                    { "Department", "Mayor's Office" },
                    { "Building", "City Hall" },
                    { "ContactEmail", "mayor@springfield.gov" }
                }
            },
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15558765432",
                CallerName = "Springfield General Hospital",
                AccountNumber = "BUS-11111",
                CallerType = CallerType.Business,
                Language = "en-US",
                Priority = 8,
                IsVip = true,
                IsBlocked = false,
                Metadata = new Dictionary<string, string>
                {
                    { "Department", "Emergency Services" },
                    { "Type", "Hospital" }
                }
            },
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15557654321",
                CallerName = "Tech Corp Main Office",
                AccountNumber = "BUS-22222",
                CallerType = CallerType.Business,
                Language = "en-US",
                Priority = 0,
                IsVip = false,
                IsBlocked = false
            },

            // === GOVERNMENT/INTERNAL ===
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15551112222",
                CallerName = "Fire Station 5",
                AccountNumber = "GOV-FIRE-5",
                CallerType = CallerType.Internal,
                Language = "en-US",
                Priority = 10,
                IsVip = true,
                IsBlocked = false,
                Metadata = new Dictionary<string, string>
                {
                    { "Station", "5" },
                    { "Type", "Fire Department" },
                    { "DispatchExtension", "5500" }
                }
            },
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15552223333",
                CallerName = "Police Precinct 12",
                AccountNumber = "GOV-POLICE-12",
                CallerType = CallerType.Internal,
                Language = "en-US",
                Priority = 10,
                IsVip = true,
                IsBlocked = false,
                Metadata = new Dictionary<string, string>
                {
                    { "Precinct", "12" },
                    { "Type", "Police Department" },
                    { "DispatchExtension", "1200" }
                }
            },
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15553334444",
                CallerName = "Public Works Department",
                AccountNumber = "GOV-PW-001",
                CallerType = CallerType.Government,
                Language = "en-US",
                Priority = 3,
                IsVip = false,
                IsBlocked = false
            },

            // === EMERGENCY SERVICES ===
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15554445555",
                CallerName = "Emergency Dispatch Center",
                AccountNumber = "EMERGENCY-DISPATCH",
                CallerType = CallerType.Emergency,
                Language = "en-US",
                Priority = 10,
                IsVip = true,
                IsBlocked = false,
                Metadata = new Dictionary<string, string>
                {
                    { "Type", "Emergency Dispatch" },
                    { "24/7", "true" }
                }
            },

            // === BLOCKED CALLERS ===
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15559999999",
                CallerName = "Spam Caller",
                CallerType = CallerType.Unknown,
                Language = "en-US",
                Priority = -10,
                IsVip = false,
                IsBlocked = true,
                Metadata = new Dictionary<string, string>
                {
                    { "BlockReason", "Repeated non-emergency calls" },
                    { "BlockedDate", "2026-02-15" }
                }
            },

            // === VIP RESIDENTIAL ===
            new AniRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15556667777",
                CallerName = "Dr. Sarah Johnson",
                AccountNumber = "VIP-RES-789",
                CallerType = CallerType.Residential,
                Language = "en-US",
                Priority = 7,
                IsVip = true,
                IsBlocked = false,
                Metadata = new Dictionary<string, string>
                {
                    { "Reason", "Medical emergency history" },
                    { "Notes", "Requires priority routing" }
                }
            }
        };
    }

    /// <summary>
    /// Generates comprehensive test ALI (Automatic Location Identification) records
    /// </summary>
    public static List<AliRecord> GenerateTestAliRecords()
    {
        return new List<AliRecord>
        {
            // === RESIDENTIAL LOCATIONS ===
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15551234567",
                Address = new Address
                {
                    Street = "123 Main Street",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22150",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7892,
                    Longitude = -77.0469
                },
                LocationType = LocationType.Residential,
                ServiceArea = "Springfield Fire District 3",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "AccessNotes", "Single family home, front door accessible" },
                    { "SpecialNeeds", "None" }
                }
            },
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15552345678",
                Address = new Address
                {
                    Street = "456 Oak Avenue, Apt 3B",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22151",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7901,
                    Longitude = -77.0521
                },
                LocationType = LocationType.Residential,
                ServiceArea = "Springfield Fire District 1",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "AccessNotes", "Apartment building, 3rd floor, unit 3B" },
                    { "BuildingAccess", "Key code: 1234#" }
                }
            },
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15553456789",
                Address = new Address
                {
                    Street = "789 Elm Street",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22152",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7856,
                    Longitude = -77.0398
                },
                LocationType = LocationType.Residential,
                ServiceArea = "Springfield Fire District 2",
                Timezone = "America/New_York",
                Region = "Northern Virginia"
            },

            // === BUSINESS LOCATIONS ===
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15559876543",
                Address = new Address
                {
                    Street = "1 City Hall Plaza",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22153",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7912,
                    Longitude = -77.0477
                },
                LocationType = LocationType.Commercial,
                ServiceArea = "Springfield Fire District 1",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "BuildingType", "Government - City Hall" },
                    { "Floors", "5" },
                    { "Occupancy", "200-500 people during business hours" },
                    { "EmergencyContact", "Building Security: +15551234000" }
                }
            },
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15558765432",
                Address = new Address
                {
                    Street = "200 Medical Center Drive",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22154",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7945,
                    Longitude = -77.0512
                },
                LocationType = LocationType.Commercial,
                ServiceArea = "Springfield Fire District 4",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "BuildingType", "Hospital - 24/7 Emergency Services" },
                    { "Floors", "8" },
                    { "SpecialInstructions", "Hospital has own emergency response team" },
                    { "EmergencyEntrance", "SW corner, ambulance bay" }
                }
            },
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15557654321",
                Address = new Address
                {
                    Street = "5000 Tech Park Boulevard, Suite 300",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22155",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7823,
                    Longitude = -77.0556
                },
                LocationType = LocationType.Commercial,
                ServiceArea = "Springfield Fire District 2",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "BuildingType", "Office Building" },
                    { "Suite", "300" },
                    { "Floor", "3" }
                }
            },

            // === GOVERNMENT/INTERNAL LOCATIONS ===
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15551112222",
                Address = new Address
                {
                    Street = "500 Fire Department Road, Station 5",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22156",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7867,
                    Longitude = -77.0445
                },
                LocationType = LocationType.Commercial,
                ServiceArea = "Springfield Fire District 5",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "BuildingType", "Fire Station" },
                    { "StationNumber", "5" },
                    { "24/7", "true" }
                }
            },
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15552223333",
                Address = new Address
                {
                    Street = "1200 Police Plaza, Precinct 12",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22157",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7934,
                    Longitude = -77.0401
                },
                LocationType = LocationType.Commercial,
                ServiceArea = "Springfield Police District 12",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "BuildingType", "Police Station" },
                    { "PrecinctNumber", "12" },
                    { "24/7", "true" }
                }
            },
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15553334444",
                Address = new Address
                {
                    Street = "300 Government Center",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22158",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7889,
                    Longitude = -77.0489
                },
                LocationType = LocationType.Commercial,
                ServiceArea = "Springfield Fire District 1",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "BuildingType", "Government Office" },
                    { "Department", "Public Works" }
                }
            },

            // === EMERGENCY SERVICE LOCATION ===
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15554445555",
                Address = new Address
                {
                    Street = "9-1-1 Emergency Communications Center",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22159",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7923,
                    Longitude = -77.0434
                },
                LocationType = LocationType.Commercial,
                ServiceArea = "All Districts - Central Dispatch",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "BuildingType", "Emergency Dispatch Center" },
                    { "24/7", "true" },
                    { "Redundancy", "Backup power and communications" }
                }
            },

            // === VIP RESIDENTIAL ===
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15556667777",
                Address = new Address
                {
                    Street = "777 Highland Drive",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22160",
                    Country = "US"
                },
                Coordinates = new GeoCoordinates
                {
                    Latitude = 38.7978,
                    Longitude = -77.0523
                },
                LocationType = LocationType.Residential,
                ServiceArea = "Springfield Fire District 3",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "AccessNotes", "Gated community, code required" },
                    { "SpecialNeeds", "Medical equipment on premises" },
                    { "Priority", "VIP - Expedited response required" }
                }
            },

            // === MOBILE/VOIP EXAMPLE (No precise coordinates) ===
            new AliRecord
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "+15559999999",
                Address = new Address
                {
                    Street = "Unknown",
                    City = "Springfield",
                    State = "VA",
                    ZipCode = "22150",
                    Country = "US"
                },
                LocationType = LocationType.VoIP,
                ServiceArea = "Unknown - VoIP caller",
                Timezone = "America/New_York",
                Region = "Northern Virginia",
                Metadata = new Dictionary<string, string>
                {
                    { "Type", "VoIP - Location may be inaccurate" },
                    { "Status", "Blocked caller" }
                }
            }
        };
    }
}
