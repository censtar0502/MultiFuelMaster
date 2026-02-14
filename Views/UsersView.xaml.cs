using System.Windows.Controls;

namespace MultiFuelMaster.Views
{
    public partial class UsersView : UserControl
    {
        public UsersView()
        {
            InitializeComponent();
            
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is ViewModels.UsersViewModel vm)
                {
                    PasswordBox.PasswordChanged += (sender, args) =>
                    {
                        vm.Password = PasswordBox.Password;
                    };
                }
            };
        }
    }
}