namespace BankSystem.Web.Controllers
{
    using Areas.MoneyTransfers.Models;
    using AutoMapper;
    using Common;
    using Infrastructure.Extensions;
    using Microsoft.AspNetCore.Mvc;
    using Models.BankAccount;
    using Services.Interfaces;
    using Services.Models.BankAccount;
    using Services.Models.MoneyTransfer;
    using System.Linq;
    using System.Threading.Tasks;

    public class BankAccountsController : BaseController
    {
        private const int ItemsPerPage = 10;

        private readonly IBankAccountService bankAccountService;
        private readonly IBankConfigurationHelper bankConfigurationHelper;
        private readonly IMoneyTransferService moneyTransferService;
        private readonly IUserService userService;

        public BankAccountsController(
            IBankAccountService bankAccountService,
            IUserService userService,
            IMoneyTransferService moneyTransferService, IBankConfigurationHelper bankConfigurationHelper)
        {
            this.bankAccountService = bankAccountService;
            this.userService = userService;
            this.moneyTransferService = moneyTransferService;
            this.bankConfigurationHelper = bankConfigurationHelper;
        }

        public IActionResult Create()
        {
            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(BankAccountCreateBindingModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            var serviceModel = Mapper.Map<BankAccountCreateServiceModel>(model);
            serviceModel.UserId = await this.userService.GetUserIdByUsernameAsync(this.User.Identity.Name);

            var accountId = await this.bankAccountService.CreateAsync(serviceModel);
            if (accountId == null)
            {
                this.ShowErrorMessage(NotificationMessages.BankAccountCreateError);

                return this.View(model);
            }

            this.ShowSuccessMessage(NotificationMessages.BankAccountCreated);
            return this.RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Details(string id, int pageIndex = 1)
        {
            var account = await this.bankAccountService.GetByIdAsync<BankAccountDetailsServiceModel>(id);
            if (account == null ||
                account.UserUserName != this.User.Identity.Name)
            {
                return this.Forbid();
            }

            var transfers = (await this.moneyTransferService
                    .GetAllMoneyTransfersForAccountAsync<MoneyTransferListingServiceModel>(id))
                .Select(Mapper.Map<MoneyTransferListingDto>)
                .ToPaginatedList(pageIndex, ItemsPerPage);

            var viewModel = Mapper.Map<BankAccountDetailsViewModel>(account);
            viewModel.MoneyTransfers = transfers;
            viewModel.BankName = this.bankConfigurationHelper.BankName;
            viewModel.BankCode = this.bankConfigurationHelper.UniqueIdentifier;

            return this.View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeAccountNameAsync(string accountId, string name)
        {
            if (name == null)
            {
                return this.Ok(new
                {
                    success = false
                });
            }

            var account = await this.bankAccountService.GetByIdAsync<BankAccountDetailsServiceModel>(accountId);
            if (account == null ||
                account.UserUserName != this.User.Identity.Name)
            {
                return this.Ok(new
                {
                    success = false
                });
            }

            bool isSuccessful = await this.bankAccountService.ChangeAccountNameAsync(accountId, name);

            return this.Ok(new
            {
                success = isSuccessful
            });
        }
    }
}