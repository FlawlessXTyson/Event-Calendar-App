import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private baseUrl = 'http://localhost:5031/api/Authentication';

  constructor(private http: HttpClient) { }

  //register
  register(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, data);
  }

  //login
  login(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/login`, data);
  }

  //save token
  saveToken(token: string) {
    localStorage.setItem('token', token);
  }

  //get token
  getToken() {
    return localStorage.getItem('token');
  }

  //check login
  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  //logout
  logout() {
    localStorage.removeItem('token');
  }
}