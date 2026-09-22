using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.Trainers;
using gym_management_server.Entities.Trainers;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.Trainers;

namespace gym_management_server.Services.Trainers
{
    public class TrainerService
    {
        private readonly ITrainerRepository _trainerRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public TrainerService(ITrainerRepository trainerRepository, GymManagementServiceMapObjects mapObjects)
        {
            _trainerRepository = trainerRepository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<TrainerOutput>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _trainerRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(e => _mapObjects.MapObjects<Trainer, TrainerOutput>(e)).ToList();
            return new PagedResult<TrainerOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<TrainerOutput?> GetByIdAsync(Guid id)
        {
            var trainer = await _trainerRepository.GetByIdAsync(id);
            if (trainer == null) return null;

            return _mapObjects.MapObjects<Trainer, TrainerOutput>(trainer);
        }

        public async Task<TrainerOutput> CreateAsync(TrainerInput input)
        {
            var trainer = new Trainer();
            trainer.Id = Guid.NewGuid();
            trainer.FullName = input.FullName;
            trainer.PhoneNumber = input.PhoneNumber;
            trainer.Email = input.Email;
            trainer.Specialty = input.Specialty;
            trainer.HourlyRate = input.HourlyRate;
            trainer.Status = input.Status;
            trainer.Notes = input.Notes;

            await _trainerRepository.AddAsync(trainer);
            return _mapObjects.MapObjects<Trainer, TrainerOutput>(trainer);
        }

        public async Task<TrainerOutput?> UpdateAsync(Guid id, TrainerInput input)
        {
            var trainer = await _trainerRepository.GetByIdAsync(id);
            if (trainer == null) return null;

            trainer.FullName = input.FullName;
            trainer.PhoneNumber = input.PhoneNumber;
            trainer.Email = input.Email;
            trainer.Specialty = input.Specialty;
            trainer.HourlyRate = input.HourlyRate;
            trainer.Status = input.Status;
            trainer.Notes = input.Notes;

            await _trainerRepository.UpdateAsync(trainer);
            return _mapObjects.MapObjects<Trainer, TrainerOutput>(trainer);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var trainer = await _trainerRepository.GetByIdAsync(id);
            if (trainer == null) return false;

            await _trainerRepository.DeleteAsync(id);
            return true;
        }

        public async Task<byte[]> ExportAsync() =>
            ExcelWriter.Write(TrainerSheet.Export, await _trainerRepository.GetForExportAsync());
    }
}
