import { bootstrapApplication } from '@angular/platform-browser'; // main functions to start our angular app
import { AppComponent } from './app/app.component'; //root component _ui starts from here 
import { appConfig } from './app/app.config'; // contains providers,services,rotung,interceptor.....

bootstrapApplication(AppComponent, appConfig).catch(err => console.error(err)); // starts angular app using appcmp with given config if app fails meanns logs errors 
