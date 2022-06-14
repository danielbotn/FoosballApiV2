using System;
using FoosballApi.Models.DoubleLeagueMatches;

namespace FoosballApi.Dtos.DoubleLeagueMatches
{
    public class AllMatchesModelReadDto
    {
        public int Id { get; set; }
        public int TeamOneId { get; set; }
        public int TeamTwoId { get; set; }
        public int LeagueId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TeamOneScore { get; set; }
        public int TeamTwoScore { get; set; }
        public bool MatchStarted { get; set; }
        public bool MatchEnded { get; set; }
        public bool MatchPaused { get; set; }
        public string TotalPlayingTime { get; set; }
        public TeamModel[] TeamOne { get; set; }
        public TeamModel[] TeamTwo { get; set; }
    }
}