using AutoMapper;
using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.Matches;
using FoosballApi.Models.Other;
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
        private readonly IFreehandDoubleMatchService _freehandDoubleMatchService;
        private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IDoubleLeaugeMatchService _doubleLeagueMatchService;
        private readonly IMapper _mapper;
        public MatchService(
            IHttpContextAccessor httpContextAccessor, 
            IFreehandMatchService freehandMatchService, 
            IMapper mapper, 
            IFreehandDoubleMatchService freehandDoubleMatchService,
            ISingleLeagueMatchService singleLeagueMatchService,
            IDoubleLeaugeMatchService doubleLeaugeMatchService)
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif

            _httpContextAccessor = httpContextAccessor;
            _freehandMatchService = freehandMatchService;
            _freehandDoubleMatchService = freehandDoubleMatchService;
            _singleLeagueMatchService = singleLeagueMatchService;
            _doubleLeagueMatchService = doubleLeaugeMatchService;
            _mapper = mapper;
        }

        public async Task<List<Match>> GetLiveMatches()
        {
            List<Match> result = new();
            string organisationId = _httpContextAccessor.HttpContext.User.FindFirst("CurrentOrganisationId").Value;
            IEnumerable<FreehandMatchModelExtended> freehandMatches = await _freehandMatchService.GetFreehandMatchesByOrganisationId(int.Parse(organisationId));
            IEnumerable<FreehandDoubleMatchModel> freehandDoubleMatches = await _freehandDoubleMatchService.GetAllFreehandDoubleMatchesByOrganisation(int.Parse(organisationId));
            IEnumerable<SingleLeagueMatchesQuery> singleLeagueMatches = await _singleLeagueMatchService.GetAllMatchesByOrganisationId(int.Parse(organisationId));
            IEnumerable<AllMatchesModel> doubleLeagueMatches = await _doubleLeagueMatchService.GetAllMatchesByOrganisation(int.Parse(organisationId));

            var matchesMapped = _mapper.Map<List<Match>>(freehandMatches);
            var doublMatchesMapped = _mapper.Map<List<Match>>(freehandDoubleMatches);
            var singleLeagueMatchesMapped = _mapper.Map<List<Match>>(singleLeagueMatches);
            var doubleLeagueMatchesMapped = _mapper.Map<List<Match>>(doubleLeagueMatches);

            result.AddRange(matchesMapped);
            result.AddRange(doublMatchesMapped);
            result.AddRange(singleLeagueMatchesMapped);
            result.AddRange(doubleLeagueMatchesMapped);

            return result;
        }
    }
}