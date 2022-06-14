using System;
using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Dtos.Matches
{
    public class FreehandMatchCreateDto
    {
        [Required]
        public int PlayerOneId { get; set; }

        [Required]
        public int PlayerTwoId { get; set; }

        [Required]
        public int PlayerOneScore { get; set; }

        [Required]
        public int PlayerTwoScore { get; set; }

        [Required]
        public int UpTo { get; set; }

        [Required]
        public bool GameFinished { get; set; }

        [Required]
        public bool GamePaused { get; set; }
    }
}