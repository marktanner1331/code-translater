class Solution:
    ans = []

    def getLonelyNodes(self, root: Optional[TreeNode]) -> List[int]:
        def helper(root):
            if root.left is None and root.right:
                self.ans.append(root.right.val)
                return helper(root.right)
            if root.left and root.right is None:
                self.ans.append(root.left.val)
                return helper(root.left)
            if root.left and root.right:
                return helper(root.left), helper(root.right)
            if root.left is None and root.right is None:
                return

        helper(root)
        ans = self.ans
        self.ans = []
        return ans