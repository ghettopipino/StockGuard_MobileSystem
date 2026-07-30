using StockGuard.Services;
using StockGuard.Models;
using StockGuard.ViewModels;
using Firebase.Database;
using Firebase.Database.Query;

namespace StockGuard.Views;

public partial class LoginView : ContentPage
{
    private readonly FirebaseService _firebase;

    public LoginView(LoginViewModel vm, FirebaseService firebase)
    {
        InitializeComponent();
        BindingContext = vm;
        _firebase = firebase;
    }
}