using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> SearchUsersForAdminAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<User>();

        // 1. تفكيك نص البحث إلى كلمات منفصلة
        var keywords = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(k => k.Trim().ToLower())
                                 .ToList();

        if (!keywords.Any())
            return new List<User>();

        var query = _context.Users.AsNoTracking();

        // 2. بناء شرط الـ OR ديناميكياً لضمان البحث في (بداية، منتصف، نهاية) الحقل لكل كلمة
        var parameter = Expression.Parameter(typeof(User), "u");
        Expression? finalExpression = null;

        // دالة المساعدة للوصول إلى دالة Contains وتجنب الـ Null
        var stringContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;

        foreach (var keyword in keywords)
        {
            var keywordConstant = Expression.Constant(keyword, typeof(string));

            // تحديد الحقول الأربعة المستهدفة
            string[] properties = { "Name", "CityPlace", "CountryPlace", "NamePlace" };

            foreach (var propName in properties)
            {
                var propAccess = Expression.Property(parameter, propName);

                // u.Prop != null
                var notNullExp = Expression.NotEqual(propAccess, Expression.Constant(null, typeof(string)));

                // u.Prop.ToLower()
                var toLowerExp = Expression.Call(propAccess, toLowerMethod);

                // u.Prop.ToLower().Contains(keyword)
                var containsExp = Expression.Call(toLowerExp, stringContainsMethod, keywordConstant);

                // u.Prop != null && u.Prop.ToLower().Contains(keyword)
                var combinedPropExp = Expression.AndAlso(notNullExp, containsExp);

                // دمج الشروط بـ OR لضمان (تطابق كلمة واحدة في أي مكان يكفي)
                finalExpression = finalExpression == null
                    ? combinedPropExp
                    : Expression.OrElse(finalExpression, combinedPropExp);
            }
        }

        if (finalExpression != null)
        {
            var lambda = Expression.Lambda<Func<User, bool>>(finalExpression, parameter);
            query = query.Where(lambda);
        }

        return await query.ToListAsync();
    }
    public async Task<List<User>> GetAllDentistsAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Dentist)
            .OrderByDescending(u => u.CreatedAt) // عرض الأحدث أولاً
            .ToListAsync();
    }
}