using AutoMapper;
using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IMatchService 
    {
        Task<List<Match>> GetLiveMatches();
    }

    public class MatchService : IMatchService
    {
        private string _connectionString { get; }
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFreehandMatchService _freehandMatchService;
        private readonly IMapper _mapper;
        public MatchService(IHttpContextAccessor httpContextAccessor, IFreehandMatchService freehandMatchService, IMapper mapper)
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif

            _httpContextAccessor = httpContextAccessor;
            _freehandMatchService = freehandMatchService;
            _mapper = mapper;
        }

        public async Task<List<Match>> GetLiveMatches()
        {
            string organisationId = _httpContextAccessor.HttpContext.User.FindFirst("CurrentOrganisationId").Value;
            IEnumerable<FreehandMatchModelExtended> freehandMatches = await _freehandMatchService.GetFreehandMatchesByOrganisationId(int.Parse(organisationId));

            var matchesMapper = _mapper.Map<List<Match>>(freehandMatches);

            return matchesMapper;
        }
    }
}