using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Models;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    public partial class UsersViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private readonly Action _onComplete;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private ObservableCollection<UserRole> _roles = new();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private User _editingUser = new();

        [ObservableProperty]
        private bool _isEditingUser;

        [ObservableProperty]
        private bool _isAddingNewUser;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private UserRole? _selectedRole;

        [ObservableProperty]
        private UserRole _editingRole = new();

        [ObservableProperty]
        private bool _isEditingRole;

        [ObservableProperty]
        private bool _isAddingNewRole;

        public UsersViewModel(UserService userService, Action onComplete)
        {
            _userService = userService;
            _onComplete = onComplete;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                var users = await _userService.GetAllAsync();
                Users = new ObservableCollection<User>(users);

                var roles = await _userService.GetAllRolesAsync();
                Roles = new ObservableCollection<UserRole>(roles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            _onComplete?.Invoke();
        }

        [RelayCommand]
        private void AddNewUser()
        {
            EditingUser = new User
            {
                RoleId = Roles.FirstOrDefault()?.Id ?? 0,
                IsActive = true
            };
            Password = string.Empty;
            IsAddingNewUser = true;
            IsEditingUser = true;
        }

        [RelayCommand]
        private void EditUser()
        {
            if (SelectedUser == null) return;

            EditingUser = new User
            {
                Id = SelectedUser.Id,
                Login = SelectedUser.Login,
                FullName = SelectedUser.FullName,
                RoleId = SelectedUser.RoleId,
                IsActive = SelectedUser.IsActive
            };
            Password = string.Empty;
            IsAddingNewUser = false;
            IsEditingUser = true;
        }

        [RelayCommand]
        private async Task DeleteUser()
        {
            if (SelectedUser == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить пользователя '{SelectedUser.Login}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _userService.DeleteAsync(SelectedUser.Id);
                    Users.Remove(SelectedUser);
                    SelectedUser = null;
                    MessageBox.Show("Пользователь успешно удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task SaveUser()
        {
            if (string.IsNullOrWhiteSpace(EditingUser.Login))
            {
                MessageBox.Show("Пожалуйста, введите логин.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingUser.RoleId <= 0)
            {
                MessageBox.Show("Пожалуйста, выберите роль.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for duplicate login
            bool loginExists = await _userService.IsLoginExistsAsync(EditingUser.Login, IsAddingNewUser ? null : EditingUser.Id);
            if (loginExists)
            {
                MessageBox.Show("Пользователь с таким логином уже существует.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Require password for new users
            if (IsAddingNewUser && string.IsNullOrEmpty(Password))
            {
                MessageBox.Show("Пожалуйста, введите пароль.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsAddingNewUser)
                {
                    // Hash password for new users
                    EditingUser.PasswordHash = User.HashPassword(Password);
                    var created = await _userService.CreateAsync(EditingUser);
                    // Reload to get the role
                    var userWithRole = await _userService.GetByIdAsync(created.Id);
                    if (userWithRole != null)
                    {
                        Users.Add(userWithRole);
                    }
                    MessageBox.Show("Пользователь успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Update password if provided
                    if (!string.IsNullOrEmpty(Password))
                    {
                        EditingUser.PasswordHash = User.HashPassword(Password);
                    }
                    await _userService.UpdateAsync(EditingUser);

                    // Reload user with role
                    var updatedUser = await _userService.GetByIdAsync(EditingUser.Id);
                    if (updatedUser != null)
                    {
                        var index = Users.ToList().FindIndex(u => u.Id == EditingUser.Id);
                        if (index >= 0)
                        {
                            Users[index] = updatedUser;
                        }
                    }
                    MessageBox.Show("Пользователь успешно обновлён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                IsEditingUser = false;
                IsAddingNewUser = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void CancelUserEdit()
        {
            IsEditingUser = false;
            IsAddingNewUser = false;
            EditingUser = new User();
            Password = string.Empty;
        }

        [RelayCommand]
        private void AddNewRole()
        {
            EditingRole = new UserRole
            {
                IsActive = true
            };
            IsAddingNewRole = true;
            IsEditingRole = true;
        }

        [RelayCommand]
        private async Task DeleteRole()
        {
            if (SelectedRole == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить роль '{SelectedRole.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _userService.DeleteRoleAsync(SelectedRole.Id);
                    Roles.Remove(SelectedRole);
                    SelectedRole = null;
                    MessageBox.Show("Роль успешно удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task SaveRole()
        {
            if (string.IsNullOrWhiteSpace(EditingRole.Name))
            {
                MessageBox.Show("Пожалуйста, введите название роли.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsAddingNewRole)
                {
                    await _userService.CreateRoleAsync(EditingRole);
                    // Reload roles
                    var roles = await _userService.GetAllRolesAsync();
                    Roles = new ObservableCollection<UserRole>(roles);
                    MessageBox.Show("Роль успешно добавлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _userService.UpdateRoleAsync(EditingRole);
                    // Reload roles
                    var roles = await _userService.GetAllRolesAsync();
                    Roles = new ObservableCollection<UserRole>(roles);
                    MessageBox.Show("Роль успешно обновлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                IsEditingRole = false;
                IsAddingNewRole = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void CancelRoleEdit()
        {
            IsEditingRole = false;
            IsAddingNewRole = false;
            EditingRole = new UserRole();
        }
    }
}