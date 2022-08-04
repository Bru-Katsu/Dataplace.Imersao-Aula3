using C1.Win.C1TrueDBGrid;
using Dataplace.Core.Application.Services.Results;
using Dataplace.Core.Comunications;
using Dataplace.Core.Domain.Localization.Messages.Extensions;
using Dataplace.Core.Domain.Notifications;
using Dataplace.Core.win.Controls.List.Behaviors;
using Dataplace.Core.win.Controls.List.Behaviors.Contracts;
using Dataplace.Core.win.Controls.List.Configurations;
using Dataplace.Imersao.Core.Application.Orcamentos.Commands;
using Dataplace.Imersao.Core.Application.Orcamentos.Queries;
using Dataplace.Imersao.Core.Application.Orcamentos.ViewModels;
using Dataplace.Imersao.Core.Domain.Orcamentos.Enums;
using dpLibrary05.Infrastructure.Helpers.Permission;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dataplace.Imersao.Presentation.Views.Orcamentos.Tools
{
    public partial class FecharOrcamentoView : dpLibrary05.Infrastructure.UserControls.ucSymGen_ToolDialog
    {
        #region Fields
        private const int _itemSeg = 467;

        private C1TrueDBGrid _grid = new C1TrueDBGrid();
        private IListBehavior<OrcamentoViewModel, OrcamentoQuery> _orcamentoList;
        #endregion

        #region Constructor
        public FecharOrcamentoView()
        {
            InitializeComponent();
            InitializeGrid();

            _orcamentoList = new C1TrueDBGridListBehavior<OrcamentoViewModel, OrcamentoQuery>(_grid)
            .WithConfiguration(GetConfiguration());

            this.ToolConfiguration += FecharOrcamentoView_ToolConfiguration; ;
            this.BeforeProcess += FecharOrcamentoView_BeforeProcess;
            this.Process += FecharOrcamentoView_Process;
        }
        #endregion

        #region Methods
        //fiz somente dessa forma pq meu computador pessoal está sem o component one, então não dá pra arrastar o grid
        private void InitializeGrid()
        {
            gridPanel.Controls.Add(_grid);
            _grid.Dock = DockStyle.Fill;
        }
        #endregion

        #region Tool Events
        private void FecharOrcamentoView_ToolConfiguration(object sender, ToolConfigurationEventArgs e)
        {
            this.Text = "Fechar orçamentos em aberto";
            e.SecurityIdList.Add(_itemSeg);
            e.CancelButtonVisisble = true;
        }

        private void FecharOrcamentoView_BeforeProcess(object sender, BeforeProcessEventArgs e)
        {
            var permission = PermissionControl.Factory().ValidatePermission(_itemSeg, dpLibrary05.mGenerico.PermissionEnum.Execute);
            if (!permission.IsAuthorized())
            {
                e.Cancel = true;
                this.Message.Info(permission.BuildMessage());
                return;
            }

            var itensSelecionados = _orcamentoList.GetCheckedItems();
            if (itensSelecionados.Count() == 0)
            {
                e.Cancel = true;
                this.Message.Info(53727.ToMessage());
                return;
            }

            e.Parameter.Items.Add("itensSelecionados", itensSelecionados);
        }

        private async void FecharOrcamentoView_Process(object sender, ProcessEventArgs e)
        {
            if (!(e.Parameter.Items.get_Item("itensSelecionados").Value is IEnumerable<OrcamentoViewModel> itensSelecionados))
            {
                e.Cancel = true;
                return;
            }

            e.ProgressMinimum = 0;
            e.ProgressMaximum = itensSelecionados.Count();
            e.BeginProcess();
            // um a um
            foreach (var item in itensSelecionados)
            {
                using (var scope = dpLibrary05.Infrastructure.ServiceLocator.ServiceLocatorScoped.Factory())
                {
                    var command = new FecharOrcamentoCommand(item);

                    var mediator = scope.Container.GetInstance<IMediatorHandler>();
                    var notifications = scope.Container.GetInstance<INotificationHandler<DomainNotification>>();

                    await mediator.SendCommand(command);

                    item.Result = Result.ResultFactory.New(notifications.GetNotifications());
                    if (item.Result.Success)
                    {
                        item.IsSelected = false;
                        item.Situacao = OrcamentoStatusEnum.Fechado.ToDataValue();
                        e.LogBuilder.Items.Add($"Orçamento {item.NumOrcamento} fechado!", dpLibrary05.Infrastructure.Helpers.LogBuilder.LogTypeEnum.Information);
                    }
                    else
                    {
                        foreach (var notification in item.Result.Notifications)
                            e.LogBuilder.Items.Add($"Orçamento {item.NumOrcamento} - {notification.Message.Trim()}", dpLibrary05.Infrastructure.Helpers.LogBuilder.LogTypeEnum.Err);
                    }
                }

                if (e.CancellationRequested)
                    break;

                e.ProgressValue += 1;
            }

            e.EndProcess();
        }
        #endregion

        #region List Events
        private ViewModelListBuilder<OrcamentoViewModel> GetConfiguration()
        {
            var builder = new ViewModelListBuilder<OrcamentoViewModel>();
            builder.AllowFilter();
            builder.AllowSort();
            builder.HasHighlight(x =>
            {
                x.Add(orcamento => orcamento.Situacao == OrcamentoStatusEnum.Cancelado.ToDataValue(), Color.Red);
            });

            builder.WithQuery<OrcamentoQuery>(() => GetData());

            builder.Ignore(x => x.CdEmpresa);
            builder.Ignore(x => x.CdFilial);
            builder.Ignore(x => x.SqTabela);
            builder.Ignore(x => x.CdTabela);
            builder.Ignore(x => x.DtFechamento);
            builder.Ignore(x => x.CdVendedor);
            builder.Ignore(x => x.DataValidade);
            builder.Ignore(x => x.DiasValidade);


            builder.Property(x => x.NumOrcamento)
                   .HasMinWidth(100)
                   .HasCaption("#");

            builder.Property(x => x.CdCliente)
                   .HasMinWidth(100)
                   .HasCaption("Cliente");

            builder.Property(x => x.DsCliente)
                   .HasMinWidth(300)
                   .HasCaption("Razão");

            builder.Property(x => x.DtOrcamento)
                   .HasMinWidth(100)
                   .HasCaption("Data")
                   .HasFormat("d");

            builder.Property(x => x.ValotTotal)
                   .HasMinWidth(100)
                   .HasCaption("Total")
                   .HasFormat("n");

            builder.Property(x => x.Situacao)
                   .HasMinWidth(100)
                   .HasCaption("Situação")
                   .HasValueItems(x =>
                   {
                       x.Add(OrcamentoStatusEnum.Aberto.ToDataValue(), 3469.ToMessage());
                       x.Add(OrcamentoStatusEnum.Fechado.ToDataValue(), 3846.ToMessage());
                       x.Add(OrcamentoStatusEnum.Cancelado.ToDataValue(), 3485.ToMessage());
                   });

            return builder;
        }

        private OrcamentoQuery GetData()
        {
            var situacaoList = new List<OrcamentoStatusEnum>();
            if (chkAberto.Checked)
                situacaoList.Add(OrcamentoStatusEnum.Aberto);
            if (chkFechado.Checked)
                situacaoList.Add(OrcamentoStatusEnum.Fechado);
            if (chkCancelado.Checked)
                situacaoList.Add(OrcamentoStatusEnum.Cancelado);

            DateTime? dtInicio = null;
            DateTime? dtFim = null;
            if (rangeDate.Date1.Value is DateTime d)
                dtInicio = d;

            if (rangeDate.Date2.Value is DateTime d2)
                dtFim = d2;

            var query = new OrcamentoQuery() { SituacaoList = situacaoList, DtInicio = dtInicio, DtFim = dtFim };

            return query;
        }
        #endregion

        #region Control Events
        private void tsiMarcar_Click(object sender, EventArgs e)
        {
            _orcamentoList.ChangeCheckState(true);
        }

        private void tsiDesmarca_Click(object sender, EventArgs e)
        {
            _orcamentoList.ChangeCheckState(false);
        }

        private async void FecharOrcamentoView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                await _orcamentoList.LoadAsync();
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.M)
            {
                _orcamentoList.ChangeCheckState(true);
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.D)
            {
                _orcamentoList.ChangeCheckState(false);
            }
        }

        private async void btnCarregar_Click(object sender, EventArgs e)
        {
            await _orcamentoList.LoadAsync();
        }
        #endregion
    }
}
