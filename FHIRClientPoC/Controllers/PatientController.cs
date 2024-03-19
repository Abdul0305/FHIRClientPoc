using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class PatientController : ControllerBase
{
    private readonly FhirClient _fhirClient;

    public PatientController()
    {
        _fhirClient = new FhirClient(new Uri("http://localhost:8090/fhir"));
    }

    [HttpPost]
    public async Task<IActionResult> CreatePatientAsync([FromBody] PatientRequestDto patientDto)
    {
        try
        {
            var patient = new Patient
            {
                Name = new List<HumanName> { new HumanName { Text = patientDto.Name } },
                Gender = (AdministrativeGender)Enum.Parse(typeof(AdministrativeGender), patientDto.Gender, true),
                BirthDate = patientDto.BirthDate,
                Telecom = new List<ContactPoint> { new ContactPoint { Value = patientDto.Email } },
                Address = new List<Address>
                {
                    new Address
                    {
                        Line = new List<string> { patientDto.AddressLine },
                        City = patientDto.City,
                        PostalCode = patientDto.PostalCode,
                        Country = patientDto.Country
                    }
                },
                MaritalStatus = new CodeableConcept { Text = patientDto.MaritalStatus }
            };

            var createdPatient = await _fhirClient.CreateAsync(patient);
            return Ok(createdPatient);
        }
        catch (FhirOperationException ex)
        {
            return BadRequest($"Error creating patient: {ex.Message}");
        }
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatientByIdAsync(string id)
    {
        try
        {
            // Retrieve patient by ID from FHIR server
            var patient = await _fhirClient.ReadAsync<Patient>($"Patient/{id}");
            return Ok(patient);
        }
        catch (FhirOperationException ex)
        {
            return NotFound($"Patient with ID '{id}' not found: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePatientAsync(string id, [FromBody] PatientRequestDto patientDto)
    {
        try
        {
            // Retrieve existing patient from FHIR server
            var existingPatient = await _fhirClient.ReadAsync<Patient>($"Patient/{id}");

            // Update patient properties with DTO values
            existingPatient.Name = new List<HumanName> { new HumanName { Text = patientDto.Name } };
            existingPatient.Gender = (AdministrativeGender)Enum.Parse(typeof(AdministrativeGender), patientDto.Gender, true);
            existingPatient.BirthDate = patientDto.BirthDate;
            existingPatient.Telecom = new List<ContactPoint> { new ContactPoint { Value = patientDto.Email } };
            existingPatient.Address = new List<Address>
            {
                new Address
                {
                    Line = new List<string> { patientDto.AddressLine },
                    City = patientDto.City,
                    PostalCode = patientDto.PostalCode,
                    Country = patientDto.Country
                }
            };
            existingPatient.MaritalStatus = new CodeableConcept { Text = patientDto.MaritalStatus };

            var updatedPatient = await _fhirClient.UpdateAsync(existingPatient);
            return Ok(updatedPatient);
        }
        catch (FhirOperationException ex)
        {
            return BadRequest($"Error updating patient: {ex.Message}");
        }
    }


    // DELETE api/patient/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatientAsync(string id)
    {
        try
        {
            // Delete patient from FHIR server
            await _fhirClient.DeleteAsync($"Patient/{id}");
            return NoContent();
        }
        catch (FhirOperationException ex)
        {
            return NotFound($"Patient with ID '{id}' not found: {ex.Message}");
        }
    }

    // GET: api/patient
    [HttpGet]
    public async Task<IActionResult> GetAllPatientsAsync()
    {
        try
        {
            // Retrieve all patients from the FHIR server
            var bundle = await _fhirClient.SearchAsync<Patient>(new string[] { });

            var patients = bundle.Entry
                .Where(e => e.Resource is Patient)
                .Select(e => (Patient)e.Resource)
                .Select(p => new PatientResponseDto
                {
                    Id = p.Id,
                    Name = p.Name.FirstOrDefault()?.Text,
                    Gender = p.Gender.ToString(),
                    BirthDate = p.BirthDate,
                    Email = p.Telecom.FirstOrDefault()?.Value,
                    AddressLine = p.Address.FirstOrDefault()?.Line.FirstOrDefault(),
                    City = p.Address.FirstOrDefault()?.City,
                    PostalCode = p.Address.FirstOrDefault()?.PostalCode,
                    Country = p.Address.FirstOrDefault()?.Country,
                    MaritalStatus = p.MaritalStatus?.Text
                }).ToList();

            return Ok(patients);
        }
        catch (FhirOperationException ex)
        {
            return BadRequest($"Error retrieving patients: {ex.Message}");
        }
    }
}
