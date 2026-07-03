using System.Threading.Tasks;
using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories
{
    public class LabSubscriptionRepository : ILabSubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public LabSubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Lab?> GetLabWithUserAsync(int labId)
        {
            return await _context.Labs
                .Include(l => l.Owner)
                .Include(l => l.SubscriptionPayments) // 🌟 للتأكد من جلب سجلات الاشتراك السابقة للمخبر
                .FirstOrDefaultAsync(l => l.Id == labId);
        }

        public async Task AddSubscriptionPaymentAsync(LabSubscriptionPayment payment)
        {
            await _context.LabSubscriptionPayments.AddAsync(payment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLabAndUserAsync(Lab lab, User user)
        {
            _context.Labs.Update(lab);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Lab>> GetActiveSubscribedLabsAsync()
        {
            // جلب المخابر النشطة مع تضمين بيانات المستخدم والدفعات التابعة له
            return await _context.Labs
                .Include(l => l.Owner)
                .Include(l => l.SubscriptionPayments) // 🌟 تضمين جدول المدفوعات
                .Where(l => l.Owner.Status == AccountStatus.Active)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lab>> GetExpiredLabsAsync()
        {
            var now = DateTime.UtcNow;

            return await _context.Labs
                .Include(l => l.Owner)
                .Where(l => l.Owner.Status == AccountStatus.Active &&
                           l.SubscriptionEndUtc != null &&
                           l.SubscriptionEndUtc <= now)
                .ToListAsync();
        }

        // 🌟 تحديث مجموعة المخابر وتحويل حساب المستخدم المرتبط بها إلى Suspended تلقائياً
        public async Task UpdateLabsRangeAsync(IEnumerable<Lab> labs)
        {
            foreach (var lab in labs)
            {
                if (lab.Owner != null)
                {
                    // إعلام الـ DbContext صراحة بتعديل حالة المستخدم
                    _context.Entry(lab.Owner).State = EntityState.Modified;

                    // الـ EF Core سيقوم تلقائياً باكتشاف الإشعارات المضافة إلى القائمة (lab.Owner.Notifications) وإدراجها كـ Insert
                }
            }

            // حفظ جميع التعديلات والإشعارات الجديدة في عملية واحدة (Transaction)
            await _context.SaveChangesAsync();
        }
    
    public async Task<LabSubscriptionPayment?> GetLatestPaymentAsync(int labId)
        {
            return await _context.LabSubscriptionPayments
                .Where(p => p.LabId == labId)
                .OrderByDescending(p => p.PaidAtUtc)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateSubscriptionPaymentAsync(LabSubscriptionPayment payment)
        {
            _context.LabSubscriptionPayments.Update(payment);
            await _context.SaveChangesAsync();
        }
    } }
