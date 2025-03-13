import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export default function SignUpPage() {
    return (
        <div className="flex w-full h-full">
            {/* Form */}
            <div className="flex justify-center items-center h-full w-full">
                <div className="w-3/4 mx-auto p-4">
                    <form className="flex flex-col gap-6">
                        <div>
                            <h1 className="font-bold text-3xl mb-1">Sign up</h1>
                            <p>Enter your personal data to create your account.</p>
                        </div>
                        <div className="flex gap-5 w-full">
                            <div className="w-full">
                                <label htmlFor="firstname" className="font-bold">FIRST NAME</label>
                                <Input type="text" placeholder="Email" name="firstname" />
                            </div>
                            <div className="w-full">
                            <label htmlFor="lastname" className="font-bold">LAST NAME</label>
                                <Input type="text" placeholder="Email" name="lastname" />
                            </div>
                        </div>
                        <div>
                            <label htmlFor="email" className="font-bold">EMAIL</label>
                            <Input type="text" placeholder="Email" name="emal" />
                        </div>
                        <div>
                            <label htmlFor="password" className="font-bold">PASSWORD</label>
                            <Input type="password" placeholder="Password" name="password" />
                        </div>
                        <div>
                            <label htmlFor="passwordcheck" className="font-bold">RETYPE PASSWORD</label>
                            <Input type="password" placeholder="Password" name="passwordcheck" />
                            <div className="flex gap-2">
                                <input type="checkbox" name="terms" />
                                <label htmlFor="terms" className="text-sm">I agree to the <a href="termsandconditions" className="text-blue-600 underline hover:text-blue-800">terms and conditions</a>.</label>
                            </div>
                        </div>
                        <Button type="submit" className="w-full">Sign up</Button>
                    </form>
                </div>
            </div>
        </div>
        
    );
  }