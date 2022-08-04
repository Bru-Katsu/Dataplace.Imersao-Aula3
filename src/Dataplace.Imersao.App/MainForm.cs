using Dataplace.Imersao.Presentation.Views;
using Dataplace.Imersao.Presentation.Views.Orcamentos.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dataplace.Imersao.App
{
    public partial class MainForm : dpLibrary05.fMNU_Principal
    {
        public MainForm()
        {
            InitializeComponent();

            AddMenu(new ToolStripMenuItem("Totalização de Condições de Pagamento", null, (object sender, EventArgs e) => {
                Dataplace.Core.win.Views.Managers.ViewManager.ShowViewOnForm<TotalCondicaoPagamentoView>();
            }), TipoMenuEnun.Relatorio);

            AddMenu(new ToolStripMenuItem("Cancelar orçamentos em aberto", null, (object sender, EventArgs e) => {
                Dataplace.Core.win.Views.Managers.ViewManager.ShowViewOnForm<CancelarOrcamentoView>();
            }), TipoMenuEnun.Ferramenta);

            AddMenu(new ToolStripMenuItem("Fechar orçamentos em aberto", null, (object sender, EventArgs e) => {
                Dataplace.Core.win.Views.Managers.ViewManager.ShowViewOnForm<FecharOrcamentoView>();
            }), TipoMenuEnun.Ferramenta);
        }
    }
}
